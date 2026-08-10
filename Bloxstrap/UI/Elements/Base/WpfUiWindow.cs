using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();

        public WpfUiWindow()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            var finalTheme = App.Settings.Prop.Theme.GetFinal();
            _themeService.SetTheme(finalTheme == Enums.Theme.Light ? ThemeType.Light : ThemeType.Dark);

            // Override accent color for rainbow/Horrorstrap themes
            if (App.Settings.Prop.Theme is Enums.Theme.Red or Enums.Theme.Orange or Enums.Theme.Yellow or Enums.Theme.Green or Enums.Theme.Blue or Enums.Theme.Indigo or Enums.Theme.Violet or Enums.Theme.Horrorstrap)
            {
                var accentColor = App.Settings.Prop.Theme switch
                {
                    Enums.Theme.Red => Color.FromRgb(239, 68, 68),
                    Enums.Theme.Orange => Color.FromRgb(249, 115, 22),
                    Enums.Theme.Yellow => Color.FromRgb(234, 179, 8),
                    Enums.Theme.Green => Color.FromRgb(34, 197, 94),
                    Enums.Theme.Blue => Color.FromRgb(59, 130, 246),
                    Enums.Theme.Indigo => Color.FromRgb(99, 102, 241),
                    Enums.Theme.Violet => Color.FromRgb(139, 92, 246),
                    Enums.Theme.Horrorstrap => Color.FromRgb(58, 156, 234),
                    _ => throw new InvalidOperationException()
                };

                _themeService.SetAccent(accentColor);
            }
            _themeService.SetSystemAccent();

            this.Background = null;

            if (App.Settings.Prop.Theme == Enums.Theme.Custom)
            {
                if (App.Settings.Prop.CustomBackgroundMode == Enums.BackgroundMode.Image)
                {
                    ApplyCustomImageBackground();
                }
                else
                {
                    var stops = App.Settings.Prop.CustomBackgroundGradientStops;
                    if (stops.Count == 0)
                    {
                        stops.Add(new Models.Persistable.CustomGradientStop(0.0, Color.FromRgb(0x4D, 0x55, 0x60)));
                        stops.Add(new Models.Persistable.CustomGradientStop(0.5, Color.FromRgb(0x38, 0x3F, 0x47)));
                        stops.Add(new Models.Persistable.CustomGradientStop(1.0, Color.FromRgb(0x25, 0x2A, 0x30)));
                    }

                    double angle = App.Settings.Prop.CustomBackgroundGradientAngle;
                    double angleRad = angle * Math.PI / 180.0;

                    double startX = 0.5 + 0.5 * Math.Cos(angleRad + Math.PI);
                    double startY = 0.5 + 0.5 * Math.Sin(angleRad + Math.PI);
                    double endX = 0.5 + 0.5 * Math.Cos(angleRad);
                    double endY = 0.5 + 0.5 * Math.Sin(angleRad);

                    var brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(startX, startY),
                        EndPoint = new Point(endX, endY)
                    };

                    foreach (var stop in stops.OrderBy(s => s.Offset))
                        brush.GradientStops.Add(new GradientStop(stop.ToColor(), stop.Offset));

                    Application.Current.Resources["ApplicationBackground"] = brush;
                    Application.Current.Resources["ApplicationBackgroundBrush"] = brush;
                }

                Application.Current.Resources.MergedDictionaries[customThemeIndex] = new ResourceDictionary();
                return;
            }

            void ApplyCustomImageBackground()
            {
                var path = App.Settings.Prop.CustomBackgroundImagePath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Application.Current.Resources["ApplicationBackground"] = null;
                    Application.Current.Resources["ApplicationBackgroundBrush"] = null;
                    return;
                }

                try
                {
                    var imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.CacheOption = BitmapCacheOption.OnLoad;
                    imageSource.UriSource = new Uri(path);
                    imageSource.EndInit();
                    imageSource.Freeze();

                    var imageBrush = new ImageBrush
                    {
                        ImageSource = imageSource,
                        Stretch = App.Settings.Prop.CustomBackgroundImageStretch switch
                        {
                            Enums.BackgroundStretch.None => Stretch.None,
                            Enums.BackgroundStretch.Fill => Stretch.Fill,
                            Enums.BackgroundStretch.Uniform => Stretch.Uniform,
                            Enums.BackgroundStretch.UniformToFill => Stretch.UniformToFill,
                            _ => Stretch.UniformToFill
                        },
                        Opacity = App.Settings.Prop.CustomBackgroundImageOpacity
                    };

                    Application.Current.Resources["ApplicationBackground"] = imageBrush;
                    Application.Current.Resources["ApplicationBackgroundBrush"] = imageBrush;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("WpfUiWindow", $"Exception when applying custom image background: {ex.Message}");
                    Application.Current.Resources["ApplicationBackground"] = null;
                    Application.Current.Resources["ApplicationBackgroundBrush"] = null;
                }
            }

            // there doesn't seem to be a way to query the name for merged dictionaries
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(finalTheme)}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

            // Build a background brush from the selected theme so the window actually uses the preset colors.
            // The WPF UI ThemeResource extension ignores our merged theme dictionaries, so we push the value
            // into ApplicationBackground ourselves.
            if (dict["ApplicationBackgroundColor"] is Color bgColor)
            {
                var brush = new SolidColorBrush(bgColor);
                Application.Current.Resources["ApplicationBackground"] = brush;
                Application.Current.Resources["ApplicationBackgroundBrush"] = brush;
            }
            else if (dict["ApplicationBackgroundBrush"] is SolidColorBrush bgBrush)
            {
                var brush = bgBrush.Clone();
                Application.Current.Resources["ApplicationBackground"] = brush;
                Application.Current.Resources["ApplicationBackgroundBrush"] = brush;
            }
            else
            {
                Application.Current.Resources["ApplicationBackground"] = null;
                Application.Current.Resources["ApplicationBackgroundBrush"] = null;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            base.OnSourceInitialized(e);
        }
    }
}
