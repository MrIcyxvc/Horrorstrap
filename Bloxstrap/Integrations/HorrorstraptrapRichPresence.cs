using DiscordRPC;

namespace Bloxstrap.Integrations
{
    public class HorrorstraptrapRichPresence : IDisposable
    {
        private readonly DiscordRpcClient _rpcClient;
        private readonly Timestamps _startTimestamps;
        private readonly Stopwatch _uptimeStopwatch;
        private bool _disposed = false;
        private string _currentPage = "Idle";
        private string? _currentDialog = null;
        private string _lastState = "";

        public bool IsConnected => _rpcClient?.IsInitialized == true;

        public HorrorstraptrapRichPresence()
        {
            _rpcClient = new DiscordRpcClient("1525965830217531582")
            {
                // Disable skipping identical presence while diagnosing button visibility issues
                // Some Discord clients may ignore Button updates if presence is considered identical.
                SkipIdenticalPresence = false
            };

            _rpcClient.OnReady += OnReady;

            Task.Run(InitializeAsync);

            _startTimestamps = new Timestamps
            {
                Start = DateTime.UtcNow
            };

            _uptimeStopwatch = Stopwatch.StartNew();
        }

        private async Task InitializeAsync()
        {
            try
            {
                if (!_rpcClient.Initialize())
                    return;

                await Task.Delay(100);
                SetPresence();
            }
            catch
            {
                // Fail Silently
            }
        }

        private void OnReady(object sender, DiscordRPC.Message.ReadyMessage args)
        {
            App.Logger.WriteLine("HorrorstraptrapRichPresence", $"Connected as {args.User.Username}");
        }

        public void SetPage(string pageName)
        {
            if (_disposed) return;

            _currentPage = pageName;
            _currentDialog = null;
            UpdatePresence();
        }

        public void SetDialog(string dialogName)
        {
            if (_disposed) return;

            _currentDialog = dialogName;
            UpdatePresence();
        }

        public void ClearDialog()
        {
            if (_disposed) return;

            _currentDialog = null;
            UpdatePresence();
        }

        public void ResetPresence()
        {
            if (_disposed) return;

            _currentPage = "Idle";
            _currentDialog = null;
            UpdatePresence();
        }

        private void SetPresence()
        {
            UpdatePresence();
        }

        private void UpdatePresence()
        {
            if (_disposed || !_rpcClient.IsInitialized)
                return;

            string state = !string.IsNullOrEmpty(_currentDialog)
                ? $"Page: {_currentPage} | Dialog: {_currentDialog}"
                : $"Page: {_currentPage}";

            if (state == _lastState)
                return;

            _lastState = state;

                try
                {
                    var presence = new DiscordRPC.RichPresence
                    {
                        Details = "Horrorstrap",
                        State = "FastFlags Settings",
                        Timestamps = _startTimestamps,
                        Assets = new Assets
                        {
                            LargeImageKey = "horrorstrap",
                            LargeImageText = $"Horrorstrap v{App.Version}",
                            SmallImageKey = "checkmark",
                            SmallImageText = "Horrorstrap"
                        },
                        Party = new Party
                        {
                            ID = Guid.NewGuid().ToString(),
                            Size = 1,
                            Max = 5
                        },
                        Buttons = new[]
                        {
                            new Button { Label = "Join Discord", Url = "https://discord.gg/Y9TPgwvaQ5" },
                            new Button { Label = "Website", Url = "https://your-website.com" }
                        }
                    };

                    // Diagnostic logging: record what we're about to send so user can verify
                    try
                    {
                        var btnDesc = presence.Buttons != null
                            ? string.Join("; ", presence.Buttons.Select(b => $"[{b.Label}: {b.Url}]") )
                            : "(none)";

                        App.Logger.WriteLine("HorrorstraptrapRichPresence", $"Setting presence. Details='{presence.Details}' State='{presence.State}' Buttons={btnDesc}");
                    }
                    catch
                    {
                        // Ignore logging failures
                    }

                    _rpcClient.SetPresence(presence);
                }
                catch
                {
                    // Fail Silently
                }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _rpcClient.OnReady -= OnReady;

                if (_rpcClient.IsInitialized)
                {
                    _rpcClient.ClearPresence();
                }

                _rpcClient.Dispose();
                _uptimeStopwatch.Stop();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
            catch
            {
                // Fail Silently
            }

            GC.SuppressFinalize(this);
        }
    }
}