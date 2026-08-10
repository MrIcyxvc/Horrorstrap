using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class AppearancePage
    {
        public AppearancePage()
        {
            DataContext = new AppearanceViewModel(this);
            InitializeComponent();
            App.BubbleRPC?.SetPage("Appearance");

            if (DataContext is AppearanceViewModel vm)
                vm.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppearanceViewModel.SelectedLanguage) && Window.GetWindow(this) is UI.Elements.Settings.MainWindow window)
            {
                Frontend.ShowMessageBox("Language changes require a relaunch of Horrorstrap to take full effect.", System.Windows.MessageBoxImage.Information);
            }
        }

        public void CustomThemeSelection(object sender, SelectionChangedEventArgs e)
        {
            AppearanceViewModel viewModel = (AppearanceViewModel)DataContext;

            viewModel.SelectedCustomTheme = (string)((ListBox)sender).SelectedItem;
            viewModel.SelectedCustomThemeName = viewModel.SelectedCustomTheme;

            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomTheme));
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomThemeName));
        }

        private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not GradientStopViewModel vm)
                return;

            var dialog = new ColorPickerDialog(vm.Color);
            if (dialog.ShowDialog() == true)
                vm.Color = dialog.SelectedColor;
        }
    }
}