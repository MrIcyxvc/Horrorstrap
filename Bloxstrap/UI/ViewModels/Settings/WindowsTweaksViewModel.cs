using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class WindowsTweaksViewModel : NotifyPropertyChangedViewModel
    {
        public bool ForceRobloxHighPriority
        {
            get => App.Settings.Prop.ForceRobloxHighPriority;
            set
            {
                App.Settings.Prop.ForceRobloxHighPriority = value;
                OnPropertyChanged(nameof(ForceRobloxHighPriority));
            }
        }

        public bool EnableUltimatePerformancePowerPlan
        {
            get => App.Settings.Prop.EnableUltimatePerformancePowerPlan;
            set
            {
                App.Settings.Prop.EnableUltimatePerformancePowerPlan = value;
                OnPropertyChanged(nameof(EnableUltimatePerformancePowerPlan));

                if (value)
                    ActivateUltimatePerformancePowerPlan();
                else
                    RestoreBalancedPowerPlan();
            }
        }

        public bool OptimizeNetworkTraffic
        {
            get => App.Settings.Prop.OptimizeNetworkTraffic;
            set
            {
                App.Settings.Prop.OptimizeNetworkTraffic = value;
                OnPropertyChanged(nameof(OptimizeNetworkTraffic));

                if (value)
                    ApplyNetworkOptimizations();
                else
                    RevertNetworkOptimizations();
            }
        }

        public bool UseCloudflareDns
        {
            get => App.Settings.Prop.UseCloudflareDns;
            set
            {
                App.Settings.Prop.UseCloudflareDns = value;
                OnPropertyChanged(nameof(UseCloudflareDns));

                if (value)
                    ApplyCloudflareDns();
                else
                    RestoreDefaultDns();
            }
        }

        public bool TcpIpOptimizations
        {
            get => App.Settings.Prop.TcpIpOptimizations;
            set
            {
                App.Settings.Prop.TcpIpOptimizations = value;
                OnPropertyChanged(nameof(TcpIpOptimizations));

                if (value)
                    ApplyTcpIpOptimizations();
                else
                    RevertTcpIpOptimizations();
            }
        }

        public bool GamingModeContextMenu
        {
            get => App.Settings.Prop.GamingModeContextMenu;
            set
            {
                App.Settings.Prop.GamingModeContextMenu = value;
                OnPropertyChanged(nameof(GamingModeContextMenu));

                if (value)
                    AddGamingModeContextMenu();
                else
                    RemoveGamingModeContextMenu();
            }
        }

        public bool DisableCpuCoreParking
        {
            get => App.Settings.Prop.DisableCpuCoreParking;
            set
            {
                App.Settings.Prop.DisableCpuCoreParking = value;
                OnPropertyChanged(nameof(DisableCpuCoreParking));

                if (value)
                    DisableCoreParking();
                else
                    EnableCoreParking();
            }
        }

        public bool DisableGpuTelemetry
        {
            get => App.Settings.Prop.DisableGpuTelemetry;
            set
            {
                App.Settings.Prop.DisableGpuTelemetry = value;
                OnPropertyChanged(nameof(DisableGpuTelemetry));

                if (value)
                    DisableGpuTelemetryTasks();
                else
                    EnableGpuTelemetryTasks();
            }
        }

        public bool ReduceNetworkLatency
        {
            get => App.FastFlags.GetPreset("Network.Heartbeat") == "33";
            set
            {
                App.FastFlags.SetPreset("Network.Heartbeat", value ? "33" : null);
                OnPropertyChanged(nameof(ReduceNetworkLatency));
            }
        }

        public bool DisableNetworkThrottling
        {
            get => App.FastFlags.GetPreset("Network.SendRate") == "999999";
            set
            {
                App.FastFlags.SetPreset("Network.SendRate", value ? "999999" : null);
                App.FastFlags.SetPreset("Network.Throttle", value ? "999999" : null);
                OnPropertyChanged(nameof(DisableNetworkThrottling));
            }
        }

        public bool DisableTelemetry
        {
            get => App.FastFlags.GetPreset("Telemetry.Disable") == "False";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Disable", value ? "False" : null);
                OnPropertyChanged(nameof(DisableTelemetry));
            }
        }

        public bool DisableWindowsTelemetry
        {
            get => App.Settings.Prop.DisableWindowsTelemetry;
            set
            {
                App.Settings.Prop.DisableWindowsTelemetry = value;
                OnPropertyChanged(nameof(DisableWindowsTelemetry));
                ApplyWindowsTelemetry(value);
            }
        }

        public bool DisableAdvertisingId
        {
            get => App.Settings.Prop.DisableAdvertisingId;
            set
            {
                App.Settings.Prop.DisableAdvertisingId = value;
                OnPropertyChanged(nameof(DisableAdvertisingId));
                ApplyAdvertisingId(!value);
            }
        }

        public bool DisableActivityHistory
        {
            get => App.Settings.Prop.DisableActivityHistory;
            set
            {
                App.Settings.Prop.DisableActivityHistory = value;
                OnPropertyChanged(nameof(DisableActivityHistory));
                ApplyActivityHistory(!value);
            }
        }

        public bool DisableLocationServices
        {
            get => App.Settings.Prop.DisableLocationServices;
            set
            {
                App.Settings.Prop.DisableLocationServices = value;
                OnPropertyChanged(nameof(DisableLocationServices));
                ApplyLocationServices(!value);
            }
        }

        public bool DisableMouseAcceleration
        {
            get => App.Settings.Prop.DisableMouseAcceleration;
            set
            {
                App.Settings.Prop.DisableMouseAcceleration = value;
                OnPropertyChanged(nameof(DisableMouseAcceleration));

                if (value)
                    ApplyDisableMouseAcceleration();
                else
                    RestoreMouseAcceleration();
            }
        }

        private static void ActivateUltimatePerformancePowerPlan()
        {
            try
            {
                // Create the Ultimate Performance plan if it doesn't exist, then set it active.
                RunPowercfg("/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                RunPowercfg("/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ActivateUltimatePerformancePowerPlan", ex);
                Frontend.ShowMessageBox($"Failed to enable Ultimate Performance power plan: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void ApplyDisableMouseAcceleration()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\\Mouse");
                // MouseAcceleration = 0 disables enhanced pointer precision
                key?.SetValue("MouseAcceleration", "0", RegistryValueKind.String);
                // MouseSpeed 1 enables raw speed (without enhanced precision)
                key?.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                // refresh system parameters
                NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSE, 0, IntPtr.Zero, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyDisableMouseAcceleration", ex);
                Frontend.ShowMessageBox($"Failed to disable mouse acceleration: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void RestoreMouseAcceleration()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\\Mouse");
                // restore defaults: enable enhanced pointer precision
                key?.SetValue("MouseAcceleration", "1", RegistryValueKind.String);
                key?.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETMOUSE, 0, IntPtr.Zero, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::RestoreMouseAcceleration", ex);
            }
        }

        private static void RestoreBalancedPowerPlan()
        {
            try
            {
                // Balanced power plan GUID.
                RunPowercfg("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::RestoreBalancedPowerPlan", ex);
                Frontend.ShowMessageBox($"Failed to restore Balanced power plan: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void RunPowercfg(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = arguments,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Win32Exception(process.ExitCode);
        }

        #region Network Tweaks

        private static void ApplyNetworkOptimizations()
        {
            try
            {
                RunNetsh("int tcp set global autotuninglevel=disabled");
                RunNetsh("int tcp set global rss=enabled");
                RunNetsh("int tcp set global netdma=enabled");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyNetworkOptimizations", ex);
                Frontend.ShowMessageBox($"Failed to apply network optimizations: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void RevertNetworkOptimizations()
        {
            try
            {
                RunNetsh("int tcp set global autotuninglevel=normal");
                RunNetsh("int tcp set global rss=default");
                RunNetsh("int tcp set global netdma=disabled");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::RevertNetworkOptimizations", ex);
            }
        }

        private static void ApplyCloudflareDns()
        {
            try
            {
                foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                        continue;

                    RunNetsh($"interface ip set dns \"{adapter.Name}\" static 1.1.1.1");
                    RunNetsh($"interface ip add dns \"{adapter.Name}\" 1.0.0.1 index=2");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyCloudflareDns", ex);
                Frontend.ShowMessageBox($"Failed to set Cloudflare DNS: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void RestoreDefaultDns()
        {
            try
            {
                foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                        continue;

                    RunNetsh($"interface ip set dns \"{adapter.Name}\" dhcp");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::RestoreDefaultDns", ex);
            }
        }

        private static void ApplyTcpIpOptimizations()
        {
            try
            {
                RunNetsh("int tcp set global autotuninglevel=disabled");
                RunNetsh("int tcp set global rss=enabled");
                RunNetsh("int tcp set global chimney=enabled");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyTcpIpOptimizations", ex);
                Frontend.ShowMessageBox($"Failed to apply TCP/IP optimizations: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void RevertTcpIpOptimizations()
        {
            try
            {
                RunNetsh("int tcp set global autotuninglevel=normal");
                RunNetsh("int tcp set global rss=default");
                RunNetsh("int tcp set global chimney=default");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::RevertTcpIpOptimizations", ex);
            }
        }

        private static void RunNetsh(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = arguments,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Win32Exception(process.ExitCode);
        }

        #endregion

        #region Gaming Mode Context Menu

        private static void AddGamingModeContextMenu()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(@"Directory\\Background\\shell\\GamingMode");
                key?.SetValue(null, "Gaming Mode");
                key?.SetValue("Icon", "explorer.exe");
                using var commandKey = key?.CreateSubKey("command");
                commandKey?.SetValue(null, $"\"{AppContext.BaseDirectory}{App.ProjectName}.exe\" -gamingmode");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::AddGamingModeContextMenu", ex);
                Frontend.ShowMessageBox($"Failed to add gaming mode context menu: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void RemoveGamingModeContextMenu()
        {
            try
            {
                Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\\Background\\shell\\GamingMode", false);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::RemoveGamingModeContextMenu", ex);
            }
        }

        #endregion

        private static void ApplyWindowsTelemetry(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                key?.SetValue("AllowTelemetry", disable ? 0 : 1, RegistryValueKind.DWord);

                using var key2 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Privacy");
                key2?.SetValue("TailoredExperiencesWithDiagnosticDataEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyWindowsTelemetry", ex);
            }
        }

        private static void ApplyAdvertisingId(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo");
                key?.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyAdvertisingId", ex);
            }
        }

        private static void ApplyActivityHistory(bool enabled)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
                key?.SetValue("EnableActivityFeed", enabled ? 1 : 0, RegistryValueKind.DWord);
                key?.SetValue("PublishUserActivities", enabled ? 1 : 0, RegistryValueKind.DWord);
                key?.SetValue("UploadUserActivities", enabled ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyActivityHistory", ex);
            }
        }

        private static void ApplyLocationServices(bool enabled)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\lfsvc\Configuration");
                key?.SetValue("Location", enabled ? 1 : 0, RegistryValueKind.DWord);

                using var key2 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location");
                key2?.SetValue("Value", enabled ? "Allow" : "Deny", RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::ApplyLocationServices", ex);
            }
        }

        #region System Tweaks

        private static void DisableCoreParking()
        {
            try
            {
                RunPowercfg("/attributes SUB_PROCESSOR CPMINCORES -ATTRIB_HIDE");
                foreach (var scheme in GetPowerSchemes())
                {
                    RunPowercfg($"/setacvalueindex {scheme} SUB_PROCESSOR CPMINCORES 0");
                    RunPowercfg($"/setdcvalueindex {scheme} SUB_PROCESSOR CPMINCORES 0");
                }
                RunPowercfg("/setactive SCHEME_CURRENT");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::DisableCoreParking", ex);
                Frontend.ShowMessageBox($"Failed to disable CPU core parking: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void EnableCoreParking()
        {
            try
            {
                foreach (var scheme in GetPowerSchemes())
                {
                    RunPowercfg($"/setacvalueindex {scheme} SUB_PROCESSOR CPMINCORES 50");
                    RunPowercfg($"/setdcvalueindex {scheme} SUB_PROCESSOR CPMINCORES 50");
                }
                RunPowercfg("/setactive SCHEME_CURRENT");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::EnableCoreParking", ex);
            }
        }

        private static string[] GetPowerSchemes()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/list",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var schemes = new System.Collections.Generic.List<string>();
                foreach (var line in output.Split('\n'))
                {
                    int start = line.IndexOf('(');
                    int end = line.IndexOf(')');
                    if (start != -1 && end > start)
                    {
                        string guid = line.Substring(start + 1, end - start - 1).Trim();
                        if (guid.Length == 36)
                            schemes.Add(guid);
                    }
                }
                return schemes.ToArray();
            }
            catch
            {
                return new[] { "SCHEME_CURRENT" };
            }
        }

        private static void DisableGpuTelemetryTasks()
        {
            try
            {
                // NVIDIA telemetry scheduled tasks
                DisableTask(@"\NVIDIA Corporation\NvTmMon_*");
                DisableTask(@"\NVIDIA Corporation\NvTmRep_*");
                DisableTask(@"\NVIDIA Corporation\NvProfileUpdater_*");

                // AMD User Experience Program task
                DisableTask(@"\AMD\AMDLinkUpdate");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::DisableGpuTelemetryTasks", ex);
                Frontend.ShowMessageBox($"Failed to disable GPU telemetry: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static void EnableGpuTelemetryTasks()
        {
            try
            {
                EnableTask(@"\NVIDIA Corporation\NvTmMon_{4E7C5B2C-4A28-4D4A-9F3D-8A8D9A1B2C3D}");
                EnableTask(@"\NVIDIA Corporation\NvTmRep_{4E7C5B2C-4A28-4D4A-9F3D-8A8D9A1B2C3D}");
                EnableTask(@"\NVIDIA Corporation\NvProfileUpdater_{4E7C5B2C-4A28-4D4A-9F3D-8A8D9A1B2C3D}");
                EnableTask(@"\AMD\AMDLinkUpdate");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WindowsTweaksViewModel::EnableGpuTelemetryTasks", ex);
            }
        }

        private static void DisableTask(string taskPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Change /TN \"{taskPath}\" /DISABLE",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch { }
        }

        private static void EnableTask(string taskPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Change /TN \"{taskPath}\" /ENABLE",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch { }
        }

        #endregion
    }
}
