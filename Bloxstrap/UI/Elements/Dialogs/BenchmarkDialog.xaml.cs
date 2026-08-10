using System.Windows;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class BenchmarkDialog : Bloxstrap.UI.Elements.Base.WpfUiWindow
    {
        public BenchmarkDialog(string score, string tier, string preset)
        {
            InitializeComponent();
            ScoreRun.Text = score;
            TierRun.Text = tier;
            PresetRun.Text = preset;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
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
