using Bloxstrap.Integrations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    /// <summary>
    /// Interaction logic for NotifyIconMenu.xaml
    /// </summary>
    public partial class MenuContainer
    {
        // i wouldve gladly done this as mvvm but turns out that data binding just does not work with menuitems for some reason so idk this sucks

        private readonly Watcher _watcher;

        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        private ServerInformation? _serverInformationWindow;

        private ServerHistory? _gameHistoryWindow;

        private readonly System.Windows.Threading.DispatcherTimer _timer = new();

        public MenuContainer(Watcher watcher)
        {
            InitializeComponent();

            _watcher = watcher;

            if (_activityWatcher is not null)
            {
                _activityWatcher.OnLogOpen += ActivityWatcher_OnLogOpen;
                _activityWatcher.OnGameJoin += ActivityWatcher_OnGameJoin;
                _activityWatcher.OnGameLeave += ActivityWatcher_OnGameLeave;

                if (!App.Settings.Prop.UseDisableAppPatch)
                    GameHistoryMenuItem.Visibility = Visibility.Visible;
            }

            GameInformationMenuItem.Visibility = Visibility.Collapsed;
            RegionMenuItem.Visibility = Visibility.Collapsed;

            if (_activityWatcher?.IsStudio == true)
            {
                CloseProcessTextBlock.Text = "Close Roblox Studio";
            }

            if (_watcher.RichPresence is not null)
                RichPresenceMenuItem.Visibility = Visibility.Visible;

            VersionTextBlock.Text = $"{App.ProjectName} v{string.Join(".", App.Version.Split('.').Take(3))}";

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) => UpdateTimers();
            _timer.Start();
        }

        public void ShowServerInformationWindow()
        {
            if (_serverInformationWindow is null)
            {
                _serverInformationWindow = new(_watcher);
                _serverInformationWindow.Closed += (_, _) => _serverInformationWindow = null;
            }

            if (!_serverInformationWindow.IsVisible)
                _serverInformationWindow.ShowDialog();
            else
                _serverInformationWindow.Activate();
        }

        public void ActivityWatcher_OnLogOpen(object? sender, EventArgs e) =>
            Dispatcher.Invoke(() => LogTracerMenuItem.Visibility = Visibility.Visible);

        public void ActivityWatcher_OnGameJoin(object? sender, EventArgs e)
        {
            if (_activityWatcher is null)
                return;

            Dispatcher.Invoke(() =>
            {
                if (_activityWatcher.Data.ServerType == ServerType.Public)
                    InviteDeeplinkMenuItem.Visibility = Visibility.Visible;

                ServerDetailsMenuItem.Visibility = Visibility.Visible;
                GameInformationMenuItem.Visibility = Visibility.Visible;
                RegionMenuItem.Visibility = Visibility.Visible;
                GameTimeMenuItem.Visibility = Visibility.Visible;

                UpdateRegionText();
                UpdateTimers();
            });
        }

        public void ActivityWatcher_OnGameLeave(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InviteDeeplinkMenuItem.Visibility = Visibility.Collapsed;
                ServerDetailsMenuItem.Visibility = Visibility.Collapsed;
                GameInformationMenuItem.Visibility = Visibility.Collapsed;
                RegionMenuItem.Visibility = Visibility.Collapsed;
                GameTimeMenuItem.Visibility = Visibility.Collapsed;

                _serverInformationWindow?.Close();
                UpdateTimers();
            });
        }

        private void UpdateTimers()
        {
            if (_activityWatcher?.SessionStartTime == DateTime.MinValue)
                return;

            var sessionElapsed = DateTime.Now - _activityWatcher.SessionStartTime;
            SessionTimeTextBlock.Text = $"Session: {sessionElapsed:hh\\:mm\\:ss}";

            if (_activityWatcher!.InGame && _activityWatcher.Data.PlaceId != 0)
            {
                var gameElapsed = DateTime.Now - _activityWatcher.Data.TimeJoined;
                GameTimeTextBlock.Text = $"Game: {gameElapsed:hh\\:mm\\:ss}";
            }
            else
            {
                GameTimeTextBlock.Text = "Game: 00:00:00";
            }
        }

        private void UpdateRegionText()
        {
            if (_activityWatcher?.Data is null)
                return;

            string? location = null;
            if (GlobalCache.ServerLocation.TryGetValue(_activityWatcher.Data.MachineAddress, out string? cached))
                location = cached;

            RegionTextBlock.Text = string.IsNullOrEmpty(location)
                ? "Region"
                : $"Region: {location}";
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            // this is an awful hack lmao im so sorry to anyone who reads this
            // this is done to register the context menu wrapper as a tool window so it doesnt appear in the alt+tab switcher
            // https://stackoverflow.com/a/551847/11852173

            HWND hWnd = (HWND)new WindowInteropHelper(this).Handle;

            int exStyle = PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            exStyle |= 0x00000080; //NativeMethods.WS_EX_TOOLWINDOW;
            PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _timer.Stop();
            App.Logger.WriteLine("MenuContainer::Window_Closed", "Context menu container closed");
        }

        private void RichPresenceMenuItem_Click(object sender, RoutedEventArgs e) => _watcher.RichPresence?.SetVisibility(((MenuItem)sender).IsChecked);

        private void InviteDeeplinkMenuItem_Click(object sender, RoutedEventArgs e) => Clipboard.SetDataObject(_activityWatcher?.Data.GetInviteDeeplink(useRobloxUri: true));

        private void ServerDetailsMenuItem_Click(object sender, RoutedEventArgs e) => ShowServerInformationWindow();

        private void LogTracerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string? location = _activityWatcher?.LogLocation;

            if (location is not null)
                Utilities.ShellExecute(location);
        }

        private void CloseRobloxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = Frontend.ShowMessageBox(
                Strings.ContextMenu_CloseRobloxMessage,
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            _watcher.KillRobloxProcess();
        }

        private void JoinLastServerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_activityWatcher is null)
                throw new ArgumentNullException(nameof(_activityWatcher));

            if (_gameHistoryWindow is null)
            {
                _gameHistoryWindow = new(_activityWatcher);
                _gameHistoryWindow.Closed += (_, _) => _gameHistoryWindow = null;
            }

            if (!_gameHistoryWindow.IsVisible)
                _gameHistoryWindow.ShowDialog();
            else
                _gameHistoryWindow.Activate();
        }

        private void RegionMenuItem_Click(object sender, RoutedEventArgs e) => ShowServerInformationWindow();

        private void GameInformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = _activityWatcher?.Data;
            if (data is null || data.UniverseDetails?.Data is null)
                return;

            var universe = data.UniverseDetails.Data;
            string info = $"{universe.Name}\nby {universe.Creator.Name}\n\n{universe.Description}";
            Frontend.ShowMessageBox(info, MessageBoxImage.Information);
        }

        private void CloseWatcherMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = Frontend.ShowMessageBox(
                "Are you sure you want to close the Horrorstrap watcher? This will stop Discord Rich Presence and activity tracking.",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            App.Terminate();
        }
    }
}
