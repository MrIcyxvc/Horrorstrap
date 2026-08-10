using System.Windows;
using System.Windows.Media;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class ColorPickerDialog
    {
        public Color SelectedColor { get; private set; }

        public ColorPickerDialog(Color initialColor)
        {
            SelectedColor = initialColor;
            DataContext = this;
            InitializeComponent();
            Picker.SelectedColor = initialColor;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = Picker.SelectedColor;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
