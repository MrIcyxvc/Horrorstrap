using Bloxstrap.UI.ViewModels.Settings;
using System.Windows.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class WindowsTweaksPage
    {
        public WindowsTweaksPage()
        {
            DataContext = new WindowsTweaksViewModel();
            InitializeComponent();
            App.BubbleRPC?.SetPage("Windows Tweaks");
        }
    }
}
