using System.Collections.Generic;
using System.Windows;

namespace Bloxstrap.UI.Elements.Settings
{
    public partial class SimpleInputDialog : Window
    {
        public string Key => KeyTextBox.Text.Trim();
        public string Value => ValueTextBox.Text.Trim();

        public bool IsSingleEntry => !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(Value);

        public SimpleInputDialog(string title, string keyLabel, string valueLabel)
        {
            InitializeComponent();
            this.Title = title;
            KeyLabel.Text = keyLabel;
            ValueLabel.Text = valueLabel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
