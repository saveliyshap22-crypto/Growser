using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace Chroma.Browser.Services;

public sealed record UpdateInfo(Version Version, string Name, string DownloadUrl, string? ChecksumUrl, string WebUrl);

public sealed class UpdateService
{
    private const string ReleasesApi = "https://api.github.com/repos/saveliyshap22-crypto/Growser/releases/latest";
    private static readonly HttpClient Client = CreateClient();

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Client.GetAsync(ReleasesApi, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
            if (!Version.TryParse(tag.Split('-', 2)[0], out var version))
            {
                return null;
            }

            var current = typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);
            if (version <= current)
            {
                return null;
            }

            var assets = root.GetProperty("assets").EnumerateArray().ToArray();
            var installer = assets.FirstOrDefault(asset =>
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                       name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase);
            });
            if (installer.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            var checksum = assets.FirstOrDefault(asset =>
                (asset.GetProperty("name").GetString() ?? string.Empty).EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));

            return new UpdateInfo(
                version,
                root.GetProperty("name").GetString() ?? $"Chroma Browser {version}",
                installer.GetProperty("browser_download_url").GetString()!,
                checksum.ValueKind == JsonValueKind.Undefined
                    ? null
                    : checksum.GetProperty("browser_download_url").GetString(),
                root.GetProperty("html_url").GetString()!);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogService.Instance.Warn($"Update check failed: {exception.Message}");
            return null;
        }
    }

    public async Task<string> DownloadAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var target = Path.Combine(AppPaths.Temp, Path.GetFileName(new Uri(update.DownloadUrl).LocalPath));
        using var response = await Client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var length = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = File.Create(target);
        var buffer = new byte[1024 * 128];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;
            if (length is > 0)
            {
                progress?.Report((double)received / length.Value);
            }
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        if (update.ChecksumUrl is not null)
        {
            var expectedText = await Client.GetStringAsync(update.ChecksumUrl, cancellationToken).ConfigureAwait(false);
            var expected = expectedText.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];
            await using var file = File.OpenRead(target);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(target);
                throw new InvalidDataException("Update checksum validation failed.");
            }
        }

        return target;
    }

    public static void LaunchInstaller(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ChromaBrowser/0.1");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}
