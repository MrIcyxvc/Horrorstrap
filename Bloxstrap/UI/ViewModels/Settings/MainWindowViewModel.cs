using Bloxstrap.UI.Elements.About;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand OpenAboutCommand => new RelayCommand(OpenAbout);
        public ICommand OpenWebpageCommand => GlobalViewModel.OpenWebpageCommand;
        public ICommand OpenLogsFolderCommand => new RelayCommand(OpenLogsFolder);

        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);

        public ICommand SaveAndLaunchSettingsCommand => new RelayCommand(SaveAndLaunchSettings);


        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);

        public EventHandler? RequestSaveNoticeEvent;

        public EventHandler? RequestCloseWindowEvent;

        public bool GBSEnabled = App.GlobalSettings.Loaded;

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active || App.Settings.Prop.TestMode;
            set
            {
                if (value && !App.State.Prop.TestModeWarningShown)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return;

                    App.State.Prop.TestModeWarningShown = true;
                }

                App.LaunchSettings.TestModeFlag.Active = value;
                // persist user's choice so it applies on next runs
                App.Settings.Prop.TestMode = value;
                App.Settings.Save();
                App.State.Save();
            }
        }

        private void OpenAbout()
        {
            App.BubbleRPC?.SetDialog("About");
            new MainWindow().ShowDialog();
            App.BubbleRPC?.ClearDialog();
        }

        private void OpenLogsFolder()
        {
            string logsPath = Paths.Logs;

            if (!Directory.Exists(logsPath))
                Directory.CreateDirectory(logsPath);

            Utilities.ShellExecute(logsPath);
        }

        private void CloseWindow() => RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);

        private void SaveSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveSettings";

            App.Settings.Save();
            ModsViewModel.ApplyRobloxIcon();
            App.State.Save();
            App.FastFlags.Save();
            App.GlobalSettings.Save();

            foreach (var pair in App.PendingSettingTasks)
            {
                var task = pair.Value;

                if (task.Changed)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Executing pending task '{task}'");
                    task.Execute();
                }
            }

            App.PendingSettingTasks.Clear();

            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
        }

        public void SaveAndLaunchSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveAndLaunchSettings";

            SaveSettings();

            if (App.LaunchSettings.TestModeFlag.Active)
            {
                CloseWindow();
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, "Launching Roblox player");

            try
            {
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
                CloseWindow();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to launch Roblox");
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(Strings.Dialog_PlayerError_FailedLaunch, MessageBoxImage.Error);
            }
        }

    }
}