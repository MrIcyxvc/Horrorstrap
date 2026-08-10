using System.Collections.ObjectModel;

namespace Bloxstrap.Models.Persistable
{
    public class Settings
    {
        public bool AllowCookieAccess { get; set; } = false;

        // configuration
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.FluentDialog;
        public BootstrapperIcon BootstrapperIcon { get; set; } = BootstrapperIcon.IconHorrorstrap;
        public bool EnableRuntimeFlagInjector { get; set; } = false;
        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public Theme Theme { get; set; } = Theme.Dark;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeveloperMode { get; set; } = false;
        public bool ForceLocalData { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public bool AutoCloseCrashHandler { get; set; } = true;
        public bool HideBootstrapperInfo { get; set; } = false;
        public bool EnableMemoryTrimmer { get; set; } = false;
        public int MemoryTrimInterval { get; set; } = 10;
        public bool EnableMemoryThreshold { get; set; } = false;
        public int MemoryTrimThreshold { get; set; } = 0;
        public bool MultiInstanceLaunching { get; set; } = false;
        public bool ConfirmLaunches { get; set; } = true;
        public string Locale { get; set; } = "nil";
        public bool UseFastFlagManager { get; set; } = true;
        public bool WPFSoftwareRender { get; set; } = false;
        public bool UpdateRoblox { get; set; } = true;
        public bool SkipRobloxUpgrades { get; set; } = false;
        public bool UsePreviousVersion { get; set; } = false;
        public bool StaticDirectory { get; set; } = false;
        public string Channel { get; set; } = RobloxInterfaces.Deployment.DefaultChannel;
        public ChannelChangeMode ChannelChangeMode { get; set; } = ChannelChangeMode.Automatic;
        public string ChannelHash { get; set; } = "";
        public string DownloadingStringFormat { get; set; } = Strings.Bootstrapper_Status_Downloading + " {0} - {1}MB / {2}MB";
        public string? SelectedCustomTheme { get; set; } = null;
        public bool BackgroundUpdatesEnabled { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool EnableTrayModal { get; set; } = false;
        public bool LaunchOnStartup { get; set; } = false;
        public string RobloxTheme { get; set; } = "Dark";
        public RobloxIcon CustomRobloxIcon { get; set; } = RobloxIcon.Default;
        public string CustomRobloxIconLocation { get; set; } = string.Empty;
        public bool DebugDisableVersionPackageCleanup { get; set; } = false;
        public WebEnvironment WebEnvironment { get; set; } = WebEnvironment.Production;

        // custom theme background
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ObservableCollection<CustomGradientStop> CustomBackgroundGradientStops { get; set; } = new();
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double CustomBackgroundGradientAngle { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public BackgroundMode CustomBackgroundMode { get; set; } = BackgroundMode.Gradient;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? CustomBackgroundImagePath { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public BackgroundStretch CustomBackgroundImageStretch { get; set; } = BackgroundStretch.UniformToFill;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double CustomBackgroundImageOpacity { get; set; } = 1.0;

        // windows tweaks
        public bool ForceRobloxHighPriority { get; set; } = false;
        public bool EnableUltimatePerformancePowerPlan { get; set; } = false;
        public bool OptimizeNetworkTraffic { get; set; } = false;
        public bool UseCloudflareDns { get; set; } = false;
        public bool TcpIpOptimizations { get; set; } = false;
        public bool GamingModeContextMenu { get; set; } = false;
        public bool DisableCpuCoreParking { get; set; } = false;
        public bool DisableGpuTelemetry { get; set; } = false;
        public bool DisableWindowsTelemetry { get; set; } = false;
        public bool DisableAdvertisingId { get; set; } = false;
        public bool DisableActivityHistory { get; set; } = false;
        public bool DisableLocationServices { get; set; } = false;
        // mouse tweaks
        public bool DisableMouseAcceleration { get; set; } = false;

        // integration configuration
        public CleanerOptions CleanerOptions { get; set; } = CleanerOptions.Never;
        public List<string> CleanerDirectories { get; set; } = new List<string>();
        public bool EnableActivityTracking { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool ShowUsingHorrorstrapRPC { get; set; } = true;
        public bool EnableCustomStatusDisplay { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public bool ShowAccountAvatarOnly { get; set; } = false;
        public bool ShowServerDetails { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();

        // test mode persisted toggle (when true, launcher will run in test mode)
        public bool TestMode { get; set; } = false;

        // mod preset configuration
        public bool UseDisableAppPatch { get; set; } = false;
    }
}