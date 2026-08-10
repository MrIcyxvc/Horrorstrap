using Bloxstrap.Models.Persistable;
using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.Elements.Editor;
using Bloxstrap.UI.Elements.Settings;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class AppearanceViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Page _page;

        public ICommand PreviewBootstrapperCommand => new RelayCommand(PreviewBootstrapper);
        public ICommand BrowseCustomIconLocationCommand => new RelayCommand(BrowseCustomIconLocation);

        public ICommand AddCustomThemeCommand => new RelayCommand(AddCustomTheme);
        public ICommand DeleteCustomThemeCommand => new RelayCommand(DeleteCustomTheme);
        public ICommand RenameCustomThemeCommand => new RelayCommand(RenameCustomTheme);
        public ICommand EditCustomThemeCommand => new RelayCommand(EditCustomTheme);
        public ICommand ExportCustomThemeCommand => new RelayCommand(ExportCustomTheme);

        public ICommand AddGradientStopCommand => new RelayCommand(AddGradientStop);
        public ICommand RemoveGradientStopCommand => new RelayCommand<GradientStopViewModel>(RemoveGradientStop);
        public ICommand ResetGradientCommand => new RelayCommand(ResetGradient);
        public ICommand ExportGradientCommand => new RelayCommand(ExportGradient);
        public ICommand ImportGradientCommand => new RelayCommand(ImportGradient);
        public ICommand SelectBackgroundImageCommand => new RelayCommand(SelectBackgroundImage);
        public ICommand ClearBackgroundImageCommand => new RelayCommand(ClearBackgroundImage);

        private void PreviewBootstrapper()
        {
            App.BubbleRPC?.SetDialog("Preview Launcher");
            IBootstrapperDialog dialog = App.Settings.Prop.BootstrapperStyle.GetNew();

            dialog.Message = "Style preview - Click the X button at the top right to close";
            dialog.ProgressStyle = System.Windows.Forms.ProgressBarStyle.Continuous;
            dialog.ProgressMaximum = 100;
            dialog.ProgressValue = 0;
            dialog.CancelEnabled = true;

            var cts = new System.Threading.CancellationTokenSource();
            System.Threading.Tasks.Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await System.Threading.Tasks.Task.Delay(30, cts.Token);
                    int nextValue = (dialog.ProgressValue + 1) % 101;
                    dialog.ProgressValue = nextValue;
                }
            }, cts.Token);

            dialog.ShowBootstrapper();
            cts.Cancel();
            App.BubbleRPC?.ClearDialog();
        }

        private void BrowseCustomIconLocation()
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_IconFiles}|*.ico"
            };

            if (dialog.ShowDialog() != true)
                return;

            CustomIconLocation = dialog.FileName;
            OnPropertyChanged(nameof(CustomIconLocation));
        }

        public AppearanceViewModel(Page page)
        {
            _page = page;

            foreach (var entry in BootstrapperIconEx.Selections)
                Icons.Add(new BootstrapperIconEntry { IconType = entry });

            PopulateCustomThemes();
            LoadGradientStops();
        }

        public IEnumerable<Theme> Themes { get; } = Enum.GetValues(typeof(Theme)).Cast<Theme>();

        public Theme Theme
        {
            get => App.Settings.Prop.Theme;
            set
            {
                App.Settings.Prop.Theme = value;
                OnPropertyChanged(nameof(Theme));
                OnPropertyChanged(nameof(IsCustomThemeSelected));
                OnPropertyChanged(nameof(IsGradientModeSelected));
                OnPropertyChanged(nameof(IsImageModeSelected));
                SaveGradientStops();
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public bool IsCustomThemeSelected => App.Settings.Prop.Theme == Theme.Custom;
        public bool IsGradientModeSelected => IsCustomThemeSelected && App.Settings.Prop.CustomBackgroundMode == BackgroundMode.Gradient;
        public bool IsImageModeSelected => IsCustomThemeSelected && App.Settings.Prop.CustomBackgroundMode == BackgroundMode.Image;

        public IEnumerable<BackgroundMode> BackgroundModes { get; } = Enum.GetValues(typeof(BackgroundMode)).Cast<BackgroundMode>();

        public BackgroundMode BackgroundMode
        {
            get => App.Settings.Prop.CustomBackgroundMode;
            set
            {
                App.Settings.Prop.CustomBackgroundMode = value;
                OnPropertyChanged(nameof(BackgroundMode));
                OnPropertyChanged(nameof(IsGradientModeSelected));
                OnPropertyChanged(nameof(IsImageModeSelected));
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public string? BackgroundImagePath
        {
            get => App.Settings.Prop.CustomBackgroundImagePath;
            set
            {
                App.Settings.Prop.CustomBackgroundImagePath = value;
                OnPropertyChanged(nameof(BackgroundImagePath));
                OnPropertyChanged(nameof(HasBackgroundImage));
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public bool HasBackgroundImage => !string.IsNullOrEmpty(App.Settings.Prop.CustomBackgroundImagePath) && File.Exists(App.Settings.Prop.CustomBackgroundImagePath);

        public IEnumerable<BackgroundStretch> BackgroundStretches { get; } = Enum.GetValues(typeof(BackgroundStretch)).Cast<BackgroundStretch>();

        public BackgroundStretch BackgroundStretch
        {
            get => App.Settings.Prop.CustomBackgroundImageStretch;
            set
            {
                App.Settings.Prop.CustomBackgroundImageStretch = value;
                OnPropertyChanged(nameof(BackgroundStretch));
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public double BackgroundImageOpacity
        {
            get => App.Settings.Prop.CustomBackgroundImageOpacity;
            set
            {
                App.Settings.Prop.CustomBackgroundImageOpacity = Math.Clamp(value, 0.0, 1.0);
                OnPropertyChanged(nameof(BackgroundImageOpacity));
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public ObservableCollection<GradientStopViewModel> GradientStops { get; set; } = new();

        public double GradientAngle
        {
            get => App.Settings.Prop.CustomBackgroundGradientAngle;
            set
            {
                App.Settings.Prop.CustomBackgroundGradientAngle = value % 360;
                OnPropertyChanged(nameof(GradientAngle));
                SaveGradientStops();
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        private void LoadGradientStops()
        {
            GradientStops.Clear();
            if (App.Settings.Prop.CustomBackgroundGradientStops.Count == 0)
            {
                GradientStops.Add(new GradientStopViewModel(0.0, Color.FromRgb(0x4D, 0x55, 0x60)));
                GradientStops.Add(new GradientStopViewModel(0.5, Color.FromRgb(0x38, 0x3F, 0x47)));
                GradientStops.Add(new GradientStopViewModel(1.0, Color.FromRgb(0x25, 0x2A, 0x30)));
                SaveGradientStops();
            }
            else
            {
                foreach (var stop in App.Settings.Prop.CustomBackgroundGradientStops)
                {
                    var vm = new GradientStopViewModel(stop.Offset, stop.ToColor());
                    vm.PropertyChanged += OnGradientStopChanged;
                    GradientStops.Add(vm);
                }
            }

            foreach (var stop in GradientStops)
                stop.PropertyChanged += OnGradientStopChanged;
        }

        private void SaveGradientStops()
        {
            App.Settings.Prop.CustomBackgroundGradientStops.Clear();
            foreach (var stop in GradientStops.OrderBy(x => x.Offset))
                App.Settings.Prop.CustomBackgroundGradientStops.Add(new CustomGradientStop(stop.Offset, stop.Color));
        }

        private void OnGradientStopChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveGradientStops();
            ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
        }

        private void AddGradientStop()
        {
            double offset = GradientStops.Any() ? Math.Min(GradientStops.Max(x => x.Offset) + 0.1, 1.0) : 0;
            var vm = new GradientStopViewModel(offset, Color.FromRgb(0x60, 0x60, 0x60));
            vm.PropertyChanged += OnGradientStopChanged;
            GradientStops.Add(vm);
            SaveGradientStops();
            OnPropertyChanged(nameof(GradientStops));
        }

        private void RemoveGradientStop(GradientStopViewModel? vm)
        {
            if (vm is null || GradientStops.Count <= 1)
                return;
            vm.PropertyChanged -= OnGradientStopChanged;
            GradientStops.Remove(vm);
            SaveGradientStops();
            OnPropertyChanged(nameof(GradientStops));
        }

        private void ResetGradient()
        {
            GradientStops.Clear();
            GradientStops.Add(new GradientStopViewModel(0.0, Color.FromRgb(0x4D, 0x55, 0x60)));
            GradientStops.Add(new GradientStopViewModel(0.5, Color.FromRgb(0x38, 0x3F, 0x47)));
            GradientStops.Add(new GradientStopViewModel(1.0, Color.FromRgb(0x25, 0x2A, 0x30)));
            foreach (var stop in GradientStops)
                stop.PropertyChanged += OnGradientStopChanged;
            SaveGradientStops();
            ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            OnPropertyChanged(nameof(GradientStops));
            OnPropertyChanged(nameof(GradientAngle));
        }

        private void SelectBackgroundImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.webp"
            };

            if (dialog.ShowDialog() != true)
                return;

            BackgroundImagePath = dialog.FileName;
        }

        private void ClearBackgroundImage()
        {
            BackgroundImagePath = null;
        }

        private void ExportGradient()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON|*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            SaveGradientStops();
            var data = new { Angle = GradientAngle, Stops = App.Settings.Prop.CustomBackgroundGradientStops };
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void ImportGradient()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON|*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(dialog.FileName));
                var root = doc.RootElement;
                if (root.TryGetProperty("Angle", out var angleProp))
                    GradientAngle = angleProp.GetDouble();

                GradientStops.Clear();
                if (root.TryGetProperty("Stops", out var stopsProp))
                {
                    foreach (var element in stopsProp.EnumerateArray())
                    {
                        var stop = new CustomGradientStop
                        {
                            Offset = element.GetProperty("Offset").GetDouble(),
                            A = (byte)element.GetProperty("A").GetInt32(),
                            R = (byte)element.GetProperty("R").GetInt32(),
                            G = (byte)element.GetProperty("G").GetInt32(),
                            B = (byte)element.GetProperty("B").GetInt32()
                        };
                        var vm = new GradientStopViewModel(stop.Offset, stop.ToColor());
                        vm.PropertyChanged += OnGradientStopChanged;
                        GradientStops.Add(vm);
                    }
                }

                SaveGradientStops();
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
                OnPropertyChanged(nameof(GradientStops));
                OnPropertyChanged(nameof(GradientAngle));
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to import gradient: {ex.Message}", MessageBoxImage.Error);
            }
        }

        public static List<string> Languages => Locale.GetLanguages();

        public string SelectedLanguage
        {
            get => Locale.SupportedLocales[App.Settings.Prop.Locale];
            set
            {
                string identifier = Locale.GetIdentifierFromName(value);
                if (identifier == App.Settings.Prop.Locale)
                    return;

                App.Settings.Prop.Locale = identifier;
                Locale.Set(identifier);
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }

        public IEnumerable<BootstrapperStyle> Dialogs { get; } = BootstrapperStyleEx.Selections;

        public BootstrapperStyle Dialog
        {
            get => App.Settings.Prop.BootstrapperStyle;
            set
            {
                if (App.Settings.Prop.BootstrapperStyle == value)
                    return;
                bool wasCustom = App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.CustomDialog;
                bool isCustom = value == BootstrapperStyle.CustomDialog;

                App.Settings.Prop.BootstrapperStyle = value;

                OnPropertyChanged(nameof(Dialog));

                if (wasCustom != isCustom)
                {
                    OnPropertyChanged(nameof(CustomThemesExpanded));
                }
            }
        }

        public bool CustomThemesExpanded => App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.CustomDialog;

        public ObservableCollection<BootstrapperIconEntry> Icons { get; set; } = new();

        public BootstrapperIcon Icon
        {
            get => App.Settings.Prop.BootstrapperIcon;
            set => App.Settings.Prop.BootstrapperIcon = value;
        }

        public string Title
        {
            get => string.IsNullOrWhiteSpace(App.Settings.Prop.BootstrapperTitle) ? App.ProjectName : App.Settings.Prop.BootstrapperTitle;
            set => App.Settings.Prop.BootstrapperTitle = value;
        }

        public string CustomIconLocation
        {
            get => App.Settings.Prop.BootstrapperIconCustomLocation;
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (App.Settings.Prop.BootstrapperIcon == BootstrapperIcon.IconCustom)
                        App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconHorrorstrap;
                }
                else
                {
                    App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconCustom;
                }

                App.Settings.Prop.BootstrapperIconCustomLocation = value;

                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Icons));
            }
        }

        private void DeleteCustomThemeStructure(string name)
        {
            string dir = Path.Combine(Paths.CustomThemes, name);
            Directory.Delete(dir, true);
        }

        private void RenameCustomThemeStructure(string oldName, string newName)
        {
            string oldDir = Path.Combine(Paths.CustomThemes, oldName);
            string newDir = Path.Combine(Paths.CustomThemes, newName);
            Directory.Move(oldDir, newDir);
        }

        private void AddCustomTheme()
        {
            App.BubbleRPC?.SetDialog("Add Custom Launcher");
            var dialog = new AddCustomThemeDialog();
            dialog.ShowDialog();
            App.BubbleRPC?.ClearDialog();

            if (dialog.Created)
            {
                CustomThemes.Add(dialog.ThemeName);
                SelectedCustomThemeIndex = CustomThemes.Count - 1;

                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                OnPropertyChanged(nameof(IsCustomThemeSelected));

                if (dialog.OpenEditor)
                    EditCustomTheme();
            }
        }

        private void DeleteCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            try
            {
                DeleteCustomThemeStructure(SelectedCustomTheme);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::DeleteCustomTheme", ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_DeleteFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            CustomThemes.Remove(SelectedCustomTheme);

            if (CustomThemes.Any())
            {
                SelectedCustomThemeIndex = CustomThemes.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
            }

            OnPropertyChanged(nameof(IsCustomBootstrapperThemeSelected));
        }

        private void RenameCustomTheme()
        {
            const string LOG_IDENT = "AppearanceViewModel::RenameCustomTheme";

            if (SelectedCustomTheme is null || SelectedCustomTheme == SelectedCustomThemeName)
                return;

            if (string.IsNullOrEmpty(SelectedCustomThemeName))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameEmpty, MessageBoxImage.Error);
                return;
            }

            var validationResult = PathValidator.IsFileNameValid(SelectedCustomThemeName);

            if (validationResult != PathValidator.ValidationResult.Ok)
            {
                switch (validationResult)
                {
                    case PathValidator.ValidationResult.IllegalCharacter:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameIllegalCharacters, MessageBoxImage.Error);
                        break;
                    case PathValidator.ValidationResult.ReservedFileName:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameReserved, MessageBoxImage.Error);
                        break;
                    default:
                        App.Logger.WriteLine(LOG_IDENT, $"Got unhandled PathValidator::ValidationResult {validationResult}");
                        Debug.Assert(false);

                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_Unknown, MessageBoxImage.Error);
                        break;
                }

                return;
            }

            // better to check for the file instead of the directory so broken themes can be overwritten
            string path = Path.Combine(Paths.CustomThemes, SelectedCustomThemeName, "Theme.xml");
            if (File.Exists(path))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameTaken, MessageBoxImage.Error);
                return;
            }

            try
            {
                RenameCustomThemeStructure(SelectedCustomTheme, SelectedCustomThemeName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_RenameFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            int idx = CustomThemes.IndexOf(SelectedCustomTheme);
            CustomThemes[idx] = SelectedCustomThemeName;

            SelectedCustomThemeIndex = idx;
            OnPropertyChanged(nameof(SelectedCustomThemeIndex));
        }

        private void EditCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            App.BubbleRPC?.SetDialog("Edit Custom Theme");

            new BootstrapperEditorWindow(SelectedCustomTheme).ShowDialog();
            App.BubbleRPC?.ClearDialog();
        }

        private void ExportCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"{SelectedCustomTheme}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            string themeDir = Path.Combine(Paths.CustomThemes, SelectedCustomTheme);

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            foreach (var filePath in Directory.EnumerateFiles(themeDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = filePath[(themeDir.Length + 1)..];

                var entry = new ZipEntry(relativePath);
                entry.DateTime = DateTime.Now;

                zipStream.PutNextEntry(entry);

                using var fileStream = File.OpenRead(filePath);
                fileStream.CopyTo(zipStream);
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void PopulateCustomThemes()
        {
            string? selected = App.Settings.Prop.SelectedCustomTheme;

            Directory.CreateDirectory(Paths.CustomThemes);

            foreach (string directory in Directory.GetDirectories(Paths.CustomThemes))
            {
                if (!File.Exists(Path.Combine(directory, "Theme.xml")))
                    continue; // missing the main theme file, ignore

                string name = Path.GetFileName(directory);
                CustomThemes.Add(name);
            }

            if (selected != null)
            {
                int idx = CustomThemes.IndexOf(selected);

                if (idx != -1)
                {
                    SelectedCustomThemeIndex = idx;
                    OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                }
                else
                {
                    SelectedCustomTheme = null;
                }
            }
        }

        public string? SelectedCustomTheme
        {
            get => App.Settings.Prop.SelectedCustomTheme;
            set => App.Settings.Prop.SelectedCustomTheme = value;
        }

        public string SelectedCustomThemeName { get; set; } = "";

        public int SelectedCustomThemeIndex { get; set; }

        public ObservableCollection<string> CustomThemes { get; set; } = new();
        public bool IsCustomBootstrapperThemeSelected => SelectedCustomTheme is not null;
    }
}