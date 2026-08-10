using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Bloxstrap.UI.ViewModels.Dialogs
{
    internal class LanguageSelectorViewModel
    {
        public event EventHandler? CloseRequestEvent;

        public ICommand SetLocaleCommand => new RelayCommand(SetLocale);

        public static List<string> Languages => Locale.GetLanguages();

        private string _selectedLanguage = Locale.SupportedLocales[App.Settings.Prop.Locale];

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                string identifier = Locale.GetIdentifierFromName(value);
                Locale.Set(identifier);
                App.Settings.Prop.Locale = identifier;
            }
        }

        private void SetLocale()
        {
            string identifier = Locale.GetIdentifierFromName(SelectedLanguage);
            Locale.Set(identifier);
            App.Settings.Prop.Locale = identifier;
            CloseRequestEvent?.Invoke(this, new());
        }
    }
}
