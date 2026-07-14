namespace Chroma.Browser.Services;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChromaBrowser");

    public static string UserData { get; } = Path.Combine(Root, "Chromium");
    public static string Data { get; } = Path.Combine(Root, "Data");
    public static string Filters { get; } = Path.Combine(Root, "Filters");
    public static string Logs { get; } = Path.Combine(Root, "Logs");
    public static string Temp { get; } = Path.Combine(Path.GetTempPath(), "ChromaBrowser");
    public static string SettingsFile { get; } = Path.Combine(Data, "settings.json");
    public static string BrowserDataFile { get; } = Path.Combine(Data, "browser-data.json");
    public static string SessionFile { get; } = Path.Combine(Data, "session.json");
    public static string VaultFile { get; } = Path.Combine(Data, "vault.bin");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(UserData);
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(Filters);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Temp);
    }
}

