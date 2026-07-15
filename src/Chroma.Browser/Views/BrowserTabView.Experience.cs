using System.Text.Json;
using Chroma.Browser.Services;

namespace Chroma.Browser.Views;

public partial class BrowserTabView
{
    private string _experienceSignature = string.Empty;
    private bool _experienceEventsAttached;

    public event EventHandler? AudioStateChanged;

    public async Task ApplyExperienceSettingsAsync()
    {
        var core = WebView.CoreWebView2;
        if (core is null)
        {
            return;
        }

        if (!_experienceEventsAttached)
        {
            _experienceEventsAttached = true;
            core.IsDocumentPlayingAudioChanged += (_, _) => AudioStateChanged?.Invoke(this, EventArgs.Empty);
            core.IsMutedChanged += (_, _) => AudioStateChanged?.Invoke(this, EventArgs.Empty);
            core.NavigationCompleted += async (_, args) =>
            {
                if (args.IsSuccess)
                {
                    await CustomCursorService.ApplyWebCursorAsync(core, _settings);
                }
            };
        }

        var signature = $"{_settings.CursorStyle}:{_settings.CursorSize}";
        if (!string.Equals(signature, _experienceSignature, StringComparison.Ordinal))
        {
            _experienceSignature = signature;
            await core.AddScriptToExecuteOnDocumentCreatedAsync(CustomCursorService.BuildWebCursorScript(_settings));
        }

        await CustomCursorService.ApplyWebCursorAsync(core, _settings);
    }

    public async Task<bool> ToggleMediaPlaybackAsync()
    {
        var core = WebView.CoreWebView2;
        if (core is null)
        {
            return false;
        }

        const string script = """
            (async () => {
              const media = [...document.querySelectorAll('audio,video')]
                .sort((a,b) => (Number(!a.paused) - Number(!b.paused)) * -1)[0];
              if (!media) return false;
              try {
                if (media.paused) await media.play(); else media.pause();
                return true;
              } catch { return false; }
            })()
            """;

        try
        {
            var result = await core.ExecuteScriptAsync(script);
            AudioStateChanged?.Invoke(this, EventArgs.Empty);
            return bool.TryParse(result, out var changed) && changed;
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Media playback could not be toggled: {exception.Message}");
            return false;
        }
    }

    public async Task<MediaMetadata> GetMediaMetadataAsync()
    {
        var fallback = new MediaMetadata(Title, GetDisplayHost(Url), IsPlayingAudio, IsMuted);
        var core = WebView.CoreWebView2;
        if (core is null)
        {
            return fallback;
        }

        const string script = """
            (() => {
              const metadata = navigator.mediaSession && navigator.mediaSession.metadata;
              const active = [...document.querySelectorAll('audio,video')]
                .find(x => !x.paused) || [...document.querySelectorAll('audio,video')][0];
              const title = metadata?.title || active?.getAttribute('title') || document.title || location.hostname;
              const artist = metadata?.artist || metadata?.album || location.hostname;
              return { title, artist, playing: Boolean(active && !active.paused), muted: Boolean(active?.muted) };
            })()
            """;

        try
        {
            var raw = await core.ExecuteScriptAsync(script);
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return fallback;
            }

            var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
            var artist = root.TryGetProperty("artist", out var artistElement) ? artistElement.GetString() : null;
            var playing = root.TryGetProperty("playing", out var playingElement) && playingElement.GetBoolean();
            var muted = root.TryGetProperty("muted", out var mutedElement) && mutedElement.GetBoolean();
            return new MediaMetadata(
                string.IsNullOrWhiteSpace(title) ? fallback.Title : title.Trim(),
                string.IsNullOrWhiteSpace(artist) ? fallback.Artist : artist.Trim(),
                playing || IsPlayingAudio,
                muted || IsMuted);
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Media metadata could not be read: {exception.Message}");
            return fallback;
        }
    }

    private static string GetDisplayHost(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        }

        return "Chroma Browser";
    }

    public sealed record MediaMetadata(string Title, string Artist, bool IsPlaying, bool IsMuted);
}
