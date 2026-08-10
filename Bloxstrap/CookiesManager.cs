using System.Security.Cryptography;

namespace Bloxstrap
{
    public class CookiesManager
    {
        private CookieState _state = CookieState.Unknown;

        public EventHandler<CookieState>? StateChanged;
        public CookieState State
        {
            get => _state;
            set
            {
                _state = value;
                StateChanged?.Invoke(this, value);
            }
        }
        public bool Loaded => Enabled && State == CookieState.Success;
        private bool Enabled => App.Settings.Prop.AllowCookieAccess;

        private string AuthCookie = string.Empty;
        private const string AuthCookieName = ".ROBLOSECURITY";
        private const string SupportedVersion = "1";
        private const string AuthPattern = $@"\t{AuthCookieName}\t(.+?)(;|$)";
        private string CookiesPath => Path.Combine(Paths.Roblox, "LocalStorage", "RobloxCookies.dat");

        public async Task<AuthenticatedUser?> GetAuthenticated()
        {
            const string LOG_IDENT = "CookiesManager::GetAuthenticated";

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://users.roblox.com/v1/users/authenticated")
            };

            request.Headers.Add("Cookie", $".ROBLOSECURITY={AuthCookie}");

            try
            {
                HttpResponseMessage response = await App.HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                AuthenticatedUser user = JsonSerializer.Deserialize<AuthenticatedUser>(content)!;

                return user;
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get authenticated user");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task<UserChannel?> GetUserChannel(string binaryType)
        {
            const string LOG_IDENT = "CookiesManager::GetUserChannel";

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://clientsettings.roblox.com/v2/user-channel?binaryType={binaryType}")
            };

            request.Headers.Add("Cookie", $".ROBLOSECURITY={AuthCookie}");

            try
            {
                HttpResponseMessage response = await App.HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                UserChannel channelInfo = JsonSerializer.Deserialize<UserChannel>(content)!;

                return channelInfo;
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get user channel");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task<string?> GetAuthTicketAsync(long placeId)
        {
            const string LOG_IDENT = "CookiesManager::GetAuthTicketAsync";

            if (!Loaded)
            {
                App.Logger.WriteLine(LOG_IDENT, "Cannot fetch auth ticket: cookies not loaded or access not allowed");
                return null;
            }

            string? csrfToken = await GetCsrfTokenAsync();
            if (string.IsNullOrEmpty(csrfToken))
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to obtain CSRF token for auth ticket request");
                return null;
            }

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://auth.roblox.com/v1/authentication-ticket/")
            };

            request.Headers.Add("Cookie", $".ROBLOSECURITY={AuthCookie}");
            request.Headers.Add("X-CSRF-TOKEN", csrfToken);
            request.Headers.Add("Origin", "https://www.roblox.com");
            request.Headers.Referrer = new Uri($"https://www.roblox.com/games/{placeId}/");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await App.HttpClient.SendAsync(request);

                if (response.Headers.TryGetValues("rbx-authentication-ticket", out var values))
                    return values.FirstOrDefault();

                string body = await response.Content.ReadAsStringAsync();
                App.Logger.WriteLine(LOG_IDENT, $"Auth ticket request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {body}");
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get authentication ticket");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        private async Task<string?> GetCsrfTokenAsync()
        {
            const string LOG_IDENT = "CookiesManager::GetCsrfTokenAsync";

            if (!Loaded)
                return null;

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://auth.roblox.com/v1/authentication-ticket/")
            };

            request.Headers.Add("Cookie", $".ROBLOSECURITY={AuthCookie}");

            try
            {
                HttpResponseMessage response = await App.HttpClient.SendAsync(request);

                if (response.Headers.TryGetValues("x-csrf-token", out var values))
                    return values.FirstOrDefault();
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get CSRF token");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task LoadCookies()
        {
            const string LOG_IDENT = "CookiesManager::LoadCookies";

            // we use the status to infrom user about it in the menu
            if (!Enabled)
            {
                State = CookieState.NotAllowed;
                App.Logger.WriteLine(LOG_IDENT, "Cookie access not allowed");
                return;
            }

            if (!string.IsNullOrEmpty(AuthCookie))
            {
                App.Logger.WriteLine(LOG_IDENT, "Cookie was already loaded!");
                return;
            }

            if (!File.Exists(CookiesPath))
            {
                State = CookieState.NotFound;
                App.Logger.WriteLine(LOG_IDENT, "Cookie file not found");
                return;
            }

            try
            {
                string content = File.ReadAllText(CookiesPath);
                var cookies = JsonSerializer.Deserialize<RobloxCookies>(content)!;

                if (cookies.Version != SupportedVersion)
                    App.Logger.WriteLine(LOG_IDENT, $"Unknown cookie version: {cookies.Version}");

                // here we got the raw bytes data which we have to decrypt with user scope
                // from that we get raw cookies data in roblox's format
                // in our case we will regex it since all we need is auth cookie
                byte[] encryptedData = Convert.FromBase64String(cookies.Cookies);
                byte[] unencryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);

                string rawCookies = Encoding.UTF8.GetString(unencryptedData);
                Match authCookieMatch = Regex.Match(rawCookies, AuthPattern);

                if (!authCookieMatch.Success)
                {
                    State = CookieState.Invalid;
                    App.Logger.WriteLine(LOG_IDENT, "Regex failed for cookies");
                    return;
                }

                string authCookie = authCookieMatch.Groups[1].Value;
                AuthCookie = authCookie; // could use better naming

                // we test the cookie to see if its valid
                AuthenticatedUser? user = await GetAuthenticated();
                if (user is null || user?.Id == 0)
                {
                    State = CookieState.Invalid;
                    App.Logger.WriteLine(LOG_IDENT, "Cookie is invalid");
                    return;
                }

                State = CookieState.Success;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load cookie!");
                App.Logger.WriteException(LOG_IDENT, ex);

                State = CookieState.Failed;
            }

            return;
        }
    }
}
