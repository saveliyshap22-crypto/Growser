namespace Chroma.Browser.Models;

public enum ThemeMode
{
    System,
    Dark,
    Light
}

public sealed class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public string AccentColor { get; set; } = "#FF8A3D";
    public double InterfaceOpacity { get; set; } = 0.92;
    public double CornerRadius { get; set; } = 14;
    public bool UseMica { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public bool RestoreSession { get; set; } = true;
    public bool SuspendBackgroundTabs { get; set; } = true;
    public int SuspendAfterMinutes { get; set; } = 20;
    public bool HttpsFirst { get; set; } = true;
    public bool AdBlockEnabled { get; set; } = true;
    public List<string> AdBlockAllowlist { get; set; } = [];
    public bool BlockPopups { get; set; } = true;
    public bool TrackingPrevention { get; set; } = true;
    public bool EnhancedSecurity { get; set; } = true;
    public bool PasswordAutosave { get; set; } = true;
    public bool GeneralAutofill { get; set; } = true;
    public bool ClearDataOnExit { get; set; }
    public string SearchEngineName { get; set; } = "Google";
    public string SearchUrlTemplate { get; set; } = "https://www.google.com/search?q={0}";
    public string DownloadFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public bool AskDownloadLocation { get; set; }
    public bool AutoCheckUpdates { get; set; } = true;
    public string NewTabWallpaperPath { get; set; } = string.Empty;
    public string AiProviderId { get; set; } = "mistral";
    public double AiPanelWidth { get; set; } = 410;
    public bool AiPanelVisible { get; set; }
    public List<FilterSubscription> FilterSubscriptions { get; set; } = FilterSubscription.CreateDefaults();
}

public sealed class FilterSubscription
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    public static List<FilterSubscription> CreateDefaults() =>
    [
        new() { Name = "EasyList", Url = "https://easylist.to/easylist/easylist.txt" },
        new() { Name = "EasyPrivacy", Url = "https://easylist.to/easylist/easyprivacy.txt" },
        new() { Name = "uBlock filters", Url = "https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/filters.txt" },
        new() { Name = "RU AdList", Url = "https://easylist-downloads.adblockplus.org/advblock.txt" },
        new() { Name = "URLhaus", Url = "https://urlhaus.abuse.ch/downloads/hostfile/" }
    ];
}
