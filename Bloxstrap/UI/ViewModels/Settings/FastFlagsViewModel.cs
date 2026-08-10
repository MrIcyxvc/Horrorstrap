using Bloxstrap.Enums.FlagPresets;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using System.Windows.Input;
using System.Linq;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        public event EventHandler? RequestPageReloadEvent;

        public event EventHandler? OpenFlagEditorEvent;
        public event EventHandler? OpenIxpEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);
        private void OpenIxpEditor() => OpenIxpEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);
        public ICommand OpenIxpEditorCommand => new RelayCommand(OpenIxpEditor);

        public ICommand ApplyPotatoPresetCommand => new RelayCommand(() => ApplyPreset("Potato"));
        public ICommand ApplyLowPresetCommand => new RelayCommand(() => ApplyPreset("Low"));
        public ICommand ApplyUltraPresetCommand => new RelayCommand(() => ApplyPreset("Ultra"));
        public ICommand AutoDetectPresetCommand => new RelayCommand(AutoDetectPreset);

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseFastFlagManager;
            set => App.Settings.Prop.UseFastFlagManager = value;
        }

        public bool EnableRuntimeFlagInjector
        {
            get => App.Settings.Prop.EnableRuntimeFlagInjector;
            set => App.Settings.Prop.EnableRuntimeFlagInjector = value;
        }

        private static readonly Dictionary<string, Dictionary<string, object?>> PerformancePresets = new()
        {
            ["Potato"] = new Dictionary<string, object?>
            {
                { "Rendering.MSAA", "0" },
                { "Rendering.FRMQualityOverride", "1" },
                { "Rendering.DisablePostFx", "True" },
                { "Rendering.SkyGray", "True" },
                { "Rendering.PauseVoxelizer", "True" },
                { "Rendering.Grass.Max", "0" },
                { "Rendering.Shadows", "0" },
                { "Rendering.TextureQuality", "0" },
                { "Rendering.MeshQuality", "0" },
                { "Rendering.ParticleQuality", "0" },
                { "Rendering.PostFX", "False" },
                { "Rendering.AntiAliasing", "0" },
                { "Geometry.MeshLOD.Static", "0" }
            },
            ["Low"] = new Dictionary<string, object?>
            {
                { "Rendering.MSAA", "0" },
                { "Rendering.FRMQualityOverride", "7" },
                { "Rendering.DisablePostFx", null },
                { "Rendering.SkyGray", null },
                { "Rendering.PauseVoxelizer", null },
                { "Rendering.Grass.Max", "50" },
                { "Rendering.Shadows", "1" },
                { "Rendering.TextureQuality", "1" },
                { "Rendering.MeshQuality", "1" },
                { "Rendering.ParticleQuality", "1" },
                { "Rendering.PostFX", "True" },
                { "Rendering.AntiAliasing", "1" },
                { "Geometry.MeshLOD.Static", "1" }
            },
            ["Ultra"] = new Dictionary<string, object?>
            {
                { "Rendering.MSAA", "4" },
                { "Rendering.FRMQualityOverride", "21" },
                { "Rendering.DisablePostFx", null },
                { "Rendering.SkyGray", null },
                { "Rendering.PauseVoxelizer", null },
                { "Rendering.Grass.Max", "1000" },
                { "Rendering.Shadows", "5" },
                { "Rendering.TextureQuality", "3" },
                { "Rendering.MeshQuality", "3" },
                { "Rendering.ParticleQuality", "3" },
                { "Rendering.PostFX", "True" },
                { "Rendering.AntiAliasing", "4" },
                { "Geometry.MeshLOD.Static", "3" }
            }
        };

        private void ApplyPreset(string preset)
        {
            if (!PerformancePresets.TryGetValue(preset, out var flags))
                return;

            foreach (var kv in flags)
                App.FastFlags.SetPreset(kv.Key, kv.Value ?? string.Empty);

            App.FastFlags.Save();
            RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);

            Frontend.ShowMessageBox($"Applied {preset} preset. {flags.Count} FastFlags updated.", System.Windows.MessageBoxImage.Information);
        }

        private void AutoDetectPreset()
        {
            try
            {
                ulong totalRamMb = GetTotalPhysicalMemoryMb();
                int coreCount = Environment.ProcessorCount;
                var (score, tier, preset) = CalculateBenchmark(totalRamMb, coreCount);

                var dialog = new UI.Elements.Dialogs.BenchmarkDialog(score.ToString(), tier, $"{preset} ({tier})");
                bool? result = dialog.ShowDialog();
                if (result == true)
                    ApplyPreset(preset);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("FastFlagsViewModel::AutoDetectPreset", $"Auto detect failed: {ex.Message}");
                Frontend.ShowMessageBox("Auto Detect failed. Please select a preset manually.", System.Windows.MessageBoxImage.Warning);
            }
        }

        private static (int score, string tier, string preset) CalculateBenchmark(ulong totalRamMb, int coreCount)
        {
            int ramScore = totalRamMb switch
            {
                < 4096 => 20,
                < 8192 => 40,
                < 16384 => 60,
                _ => 80
            };

            int cpuScore = coreCount switch
            {
                <= 2 => 20,
                <= 4 => 40,
                <= 8 => 60,
                _ => 80
            };

            int score = Math.Min(100, (ramScore + cpuScore) / 2);

            string tier = score switch
            {
                < 30 => "Low-End",
                < 60 => "Mid-Range",
                < 85 => "High-End",
                _ => "Very High-End"
            };

            string preset = score switch
            {
                < 30 => "Potato",
                < 60 => "Low",
                _ => "Ultra"
            };

            return (score, tier, preset);
        }

        private static ulong GetTotalPhysicalMemoryMb()
        {
            try
            {
                var gcMemoryInfo = GC.GetGCMemoryInfo();
                return (ulong)(gcMemoryInfo.TotalAvailableMemoryBytes / (1024 * 1024));
            }
            catch
            {
                return 0;
            }
        }

        public IReadOnlyDictionary<MSAAMode, string?> MSAALevels => FastFlagManager.MSAAModes;

        public MSAAMode SelectedMSAALevel
        {
            get => MSAALevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.MSAA")).Key;
            set => App.FastFlags.SetPreset("Rendering.MSAA", MSAALevels[value]);
        }

        public IReadOnlyDictionary<RenderingMode, string> RenderingModes => FastFlagManager.RenderingModes;

        public RenderingMode SelectedRenderingMode
        {
            get => App.FastFlags.GetPresetEnum(RenderingModes, "Rendering.Mode", "True");
            set
            {
                RenderingMode[] DisableD3D11 = new RenderingMode[]
                {
                    RenderingMode.Vulkan,
                };

                App.FastFlags.SetPresetEnum("Rendering.Mode", value.ToString(), "True");
                App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", DisableD3D11.Contains(value) ? "True" : null);
            }
        }

        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        private static readonly string[] LODLevels = { "L0", "L12", "L23", "L34" };

        public bool FRMQualityOverrideEnabled
        {
            get => App.FastFlags.GetPreset("Rendering.FRMQualityOverride") != null;
            set
            {
                if (value)
                    FRMQualityOverride = 21;
                else
                    App.FastFlags.SetPreset("Rendering.FRMQualityOverride", null);

                OnPropertyChanged(nameof(FRMQualityOverride));
                OnPropertyChanged(nameof(FRMQualityOverrideEnabled));
            }
        }

        public int FRMQualityOverride
        {
            get => int.TryParse(App.FastFlags.GetPreset("Rendering.FRMQualityOverride"), out var x) ? x : 21;
            set
            {
                App.FastFlags.SetPreset("Rendering.FRMQualityOverride", value);

                OnPropertyChanged(nameof(FRMQualityOverride));
            }
        }

        public bool MeshQualityEnabled
        {
            get => App.FastFlags.GetPreset("Geometry.MeshLOD.Static") != null;
            set
            {
                if (value)
                {
                    // we enable level 3 by default
                    MeshQuality = 3;
                }
                else
                {
                    foreach (string level in LODLevels)
                        App.FastFlags.SetPreset($"Geometry.MeshLOD.{level}", null);

                    App.FastFlags.SetPreset("Geometry.MeshLOD.Static", null);
                }

                OnPropertyChanged(nameof(MeshQualityEnabled));
            }
        }

        public int MeshQuality
        {
            get => int.TryParse(App.FastFlags.GetPreset("Geometry.MeshLOD.Static"), out var x) ? x : 0;
            set
            {
                int clamped = Math.Clamp(value, 0, LODLevels.Length - 1);

                for (int i = 0; i < LODLevels.Length; i++)
                {
                    int lodValue = (Math.Clamp(clamped - i, 0, 3) + 1) * 250;
                    string lodLevel = LODLevels[i];

                    App.FastFlags.SetPreset($"Geometry.MeshLOD.{lodLevel}", lodValue);
                }

                App.FastFlags.SetPreset("Geometry.MeshLOD.Static", clamped);
                OnPropertyChanged(nameof(MeshQuality));
                OnPropertyChanged(nameof(MeshQualityEnabled));
            }
        }

        private static string ResetBackupPath => Path.Combine(Paths.Base, "FastFlags_Reset_Backup.json");

        public bool ResetConfiguration
        {
            get => File.Exists(ResetBackupPath);

            set
            {
                try
                {
                    if (value)
                    {
                        // Backup current flags before resetting
                        var backup = new Dictionary<string, object>(App.FastFlags.Prop);
                        File.WriteAllText(ResetBackupPath, JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));

                        App.FastFlags.Prop.Clear();
                    }
                    else
                    {
                        if (File.Exists(ResetBackupPath))
                        {
                            string json = File.ReadAllText(ResetBackupPath);
                            var restored = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                            if (restored is not null)
                            {
                                App.FastFlags.Prop.Clear();
                                foreach (var kv in restored)
                                    App.FastFlags.Prop[kv.Key] = kv.Value;
                            }

                            File.Delete(ResetBackupPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("FastFlagsViewModel::ResetConfiguration", $"Failed to toggle reset: {ex.Message}");
                }

                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool SkyGray
        {
            get => App.FastFlags.GetPreset("Rendering.SkyGray") == "True";
            set
            {
                App.FastFlags.SetPreset("Rendering.SkyGray", value ? "True" : null);
                OnPropertyChanged(nameof(SkyGray));
            }
        }
        public bool DisableGrass
        {
            get => App.FastFlags.GetPreset("Rendering.Grass.Max") == "0";
            set
            {
                object? grassValue = value ? 0 : null;

                App.FastFlags.SetPreset("Rendering.Grass", grassValue);

                OnPropertyChanged(nameof(DisableGrass));
            }
        }

        public static IReadOnlyDictionary<GrassMovementMode, string?> GrassMovementModes => new Dictionary<GrassMovementMode, string?>
        {
            { GrassMovementMode.Default, null },
            { GrassMovementMode.NoMovement, "0" },
            { GrassMovementMode.Minimal, "1" },
            { GrassMovementMode.Medium, "2" },
            { GrassMovementMode.High, "5" },
            { GrassMovementMode.Ultra, "10" }
        };

        public GrassMovementMode SelectedGrassMovementMode
        {
            get => GrassMovementModes.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.Grass.Movement")).Key;
            set
            {
                App.FastFlags.SetPreset("Rendering.Grass.Movement", GrassMovementModes[value]);
                OnPropertyChanged(nameof(SelectedGrassMovementMode));
            }
        }

        public bool PauseVoxelizer
        {
            get => App.FastFlags.GetPreset("Rendering.PauseVoxelizer") == "True";
            set
            {
                App.FastFlags.SetPreset("Rendering.PauseVoxelizer", value ? "True" : null);
                OnPropertyChanged(nameof(PauseVoxelizer));
            }
        }

            }
        }