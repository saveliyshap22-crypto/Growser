namespace Chroma.Browser.Models;

public enum ThemeMode
{
    System,
    Dark,
    Light
}

public enum InterfaceDensity
{
    Comfortable,
    Compact
}

public enum TabWidthMode
{
    Adaptive,
    Compact,
    Wide
}

public enum SurfaceShapeMode
{
    Rounded,
    Balanced,
    Square
}

public enum GlassMode
{
    Strong,
    Soft,
    Off
}

public enum CursorStyleMode
{
    System,
    WhiteDot,
    BlackDot,
    RainbowDot
}

public sealed class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public string AccentColor { get; set; } = "#FF8A3D";
    public double InterfaceOpacity { get; set; } = 0.96;
    public double GlassIntensity { get; set; } = 0.78;
    public GlassMode GlassMode { get; set; } = GlassMode.Strong;
    public SurfaceShapeMode SurfaceShape { get; set; } = SurfaceShapeMode.Rounded;
    public double CornerRadius { get; set; } = 18;
    public InterfaceDensity InterfaceDensity { get; set; } = InterfaceDensity.Comfortable;
    public TabWidthMode TabWidthMode { get; set; } = TabWidthMode.Adaptive;
    public bool ShowTabCloseButtons { get; set; } = true;
    public bool ShowAmbientGlow { get; set; } = true;
    public bool UseMica { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public CursorStyleMode CursorStyle { get; set; } = CursorStyleMode.System;
    public int CursorSize { get; set; } = 16;
    public bool MediaPillEnabled { get; set; } = true;
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
    public string AiProviderId { get; set; } = "chatgpt";
    public double AiPanelWidth { get; set; } = 430;
    public bool AiPanelVisible { get; set; }
    public bool AiIncludePageContext { get; set; }
    public int AiContextCharacterLimit { get; set; } = 12000;
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
