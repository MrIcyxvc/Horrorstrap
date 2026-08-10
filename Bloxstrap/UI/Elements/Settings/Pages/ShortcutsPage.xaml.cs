using Bloxstrap.UI.ViewModels.Settings;

using System.Windows;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ShortcutsPage.xaml
    /// </summary>
    public partial class ShortcutsPage
    {
        public ShortcutsPage()
        {
            DataContext = new ShortcutsViewModel();
            InitializeComponent();
            App.BubbleRPC?.SetPage("Shortcuts");
        }

        private async void CreateGameShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GameSearchBox") is not System.Windows.Controls.ComboBox box)
                return;

            string input = box.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                Frontend.ShowMessageBox("Please enter a game name or place ID.", MessageBoxImage.Warning);
                return;
            }

            if (!long.TryParse(input, out long placeId) || placeId <= 0)
            {
                Frontend.ShowMessageBox("Please enter a valid numeric Place ID (e.g. 4483381587).", MessageBoxImage.Warning);
                return;
            }

            try
            {
                string lnkName = $"{input} - {App.ProjectName}.lnk";
                string lnkPath = Path.Combine(Paths.Desktop, lnkName);

                string uri;
                string? ticket = await App.Cookies.GetAuthTicketAsync(placeId);

                if (!string.IsNullOrEmpty(ticket))
                {
                    string browserTrackerId = new Random().Next(1000000000, 2147483647).ToString();
                    uri = $"roblox-player:1+launchmode:play+gameinfo:{ticket}+browsertrackerid:{browserTrackerId}+robloxLocale:en_us+gameLocale:en_us+channel:";
                }
                else
                {
                    uri = $"roblox://experiences/start?placeId={placeId}";
                }

                Bloxstrap.Utility.Shortcut.Create(Paths.Process, $"\"{uri}\"", lnkPath, overwrite: true, iconPath: Path.Combine(AppContext.BaseDirectory, "Horrorstrap.ico"));

                Frontend.ShowMessageBox($"Game shortcut created on the desktop: {lnkName}", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ShortcutsPage", $"Failed to create game shortcut: {ex.Message}");
                Frontend.ShowMessageBox($"Failed to create game shortcut: {ex.Message}", MessageBoxImage.Error);
            }
        }
    }
}
