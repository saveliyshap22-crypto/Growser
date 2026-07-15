using System.Text.Json;
using System.Windows.Input;
using Chroma.Browser.Models;
using Microsoft.Web.WebView2.Core;

namespace Chroma.Browser.Services;

public static class CustomCursorService
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, CursorCacheEntry> Cache = [];

    public static Cursor GetCursor(AppSettings settings) =>
        GetCursor(settings.CursorStyle, settings.CursorSize);

    public static Cursor GetCursor(CursorStyleMode style, int requestedSize)
    {
        if (style == CursorStyleMode.System)
        {
            return Cursors.Arrow;
        }

        var size = Math.Clamp(requestedSize, 10, 24);
        var key = $"{style}:{size}";
        lock (Sync)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached.Cursor;
            }

            try
            {
                var stream = new MemoryStream(CreateCursorBytes(style, size), writable: false);
                var cursor = new Cursor(stream);
                Cache[key] = new CursorCacheEntry(stream, cursor);
                return cursor;
            }
            catch (Exception exception)
            {
                LogService.Instance.Warn($"Custom cursor could not be created: {exception.Message}");
                return Cursors.Arrow;
            }
        }
    }

    public static async Task ApplyWebCursorAsync(CoreWebView2? core, AppSettings settings)
    {
        if (core is null)
        {
            return;
        }

        try
        {
            await core.ExecuteScriptAsync(BuildWebCursorScript(settings));
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Web cursor could not be applied: {exception.Message}");
        }
    }

    public static string BuildWebCursorScript(AppSettings settings)
    {
        if (settings.CursorStyle == CursorStyleMode.System)
        {
            return "(() => document.getElementById('chroma-custom-cursor')?.remove())();";
        }

        var size = Math.Clamp(settings.CursorSize, 10, 24);
        var canvas = 32;
        var center = canvas / 2;
        var radius = Math.Clamp(size / 2d, 5, 12);
        var svg = settings.CursorStyle switch
        {
            CursorStyleMode.BlackDot =>
                $"<svg xmlns='http://www.w3.org/2000/svg' width='{canvas}' height='{canvas}' viewBox='0 0 {canvas} {canvas}'><circle cx='{center}' cy='{center}' r='{radius:0.#}' fill='black' stroke='white' stroke-width='2'/></svg>",
            CursorStyleMode.RainbowDot =>
                $"<svg xmlns='http://www.w3.org/2000/svg' width='{canvas}' height='{canvas}' viewBox='0 0 {canvas} {canvas}'><defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'><stop stop-color='#ff3158'/><stop offset='.2' stop-color='#ffb52e'/><stop offset='.4' stop-color='#fff04a'/><stop offset='.6' stop-color='#42e695'/><stop offset='.8' stop-color='#4f7cff'/><stop offset='1' stop-color='#c45cff'/></linearGradient></defs><circle cx='{center}' cy='{center}' r='{radius:0.#}' fill='url(#g)'/></svg>",
            _ =>
                $"<svg xmlns='http://www.w3.org/2000/svg' width='{canvas}' height='{canvas}' viewBox='0 0 {canvas} {canvas}'><circle cx='{center}' cy='{center}' r='{radius:0.#}' fill='white' stroke='black' stroke-width='2'/></svg>"
        };

        var data = Uri.EscapeDataString(svg);
        var css = $"html,body,body *{{cursor:url(\"data:image/svg+xml,{data}\") {center} {center},auto!important}}";
        var serializedCss = JsonSerializer.Serialize(css);
        return $$"""
            (() => {
              const apply = () => {
                let style = document.getElementById('chroma-custom-cursor');
                if (!style) {
                  style = document.createElement('style');
                  style.id = 'chroma-custom-cursor';
                  (document.head || document.documentElement).appendChild(style);
                }
                style.textContent = {{serializedCss}};
              };
              if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', apply, { once: true });
              } else {
                apply();
              }
            })();
            """;
    }

    private static byte[] CreateCursorBytes(CursorStyleMode style, int requestedSize)
    {
        const int canvas = 32;
        var center = (canvas - 1) / 2d;
        var radius = Math.Clamp(requestedSize / 2d, 5, 12);
        var pixelBytes = canvas * canvas * 4;
        var maskStride = ((canvas + 31) / 32) * 4;
        var maskBytes = maskStride * canvas;
        var imageBytes = 40 + pixelBytes + maskBytes;

        using var stream = new MemoryStream(22 + imageBytes);
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)2);
        writer.Write((ushort)1);
        writer.Write((byte)canvas);
        writer.Write((byte)canvas);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)(canvas / 2));
        writer.Write((ushort)(canvas / 2));
        writer.Write(imageBytes);
        writer.Write(22);

        writer.Write(40);
        writer.Write(canvas);
        writer.Write(canvas * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixelBytes);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        for (var sourceY = canvas - 1; sourceY >= 0; sourceY--)
        {
            for (var x = 0; x < canvas; x++)
            {
                var dx = x - center;
                var dy = sourceY - center;
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                var coverage = Math.Clamp(radius + 0.75 - distance, 0, 1);
                var borderCoverage = Math.Clamp(radius + 0.75 - distance, 0, 1) -
                                     Math.Clamp(radius - 1.55 - distance, 0, 1);

                var (red, green, blue) = style switch
                {
                    CursorStyleMode.BlackDot when borderCoverage > 0.04 => (255d, 255d, 255d),
                    CursorStyleMode.BlackDot => (0d, 0d, 0d),
                    CursorStyleMode.RainbowDot => HsvToRgb(
                        ((Math.Atan2(dy, dx) / (Math.PI * 2)) + 1 + (distance / Math.Max(radius, 1) * 0.13)) % 1,
                        0.78,
                        1),
                    _ when borderCoverage > 0.04 => (0d, 0d, 0d),
                    _ => (255d, 255d, 255d)
                };

                writer.Write((byte)Math.Clamp(blue, 0, 255));
                writer.Write((byte)Math.Clamp(green, 0, 255));
                writer.Write((byte)Math.Clamp(red, 0, 255));
                writer.Write((byte)Math.Round(coverage * 255));
            }
        }

        writer.Write(new byte[maskBytes]);
        writer.Flush();
        return stream.ToArray();
    }

    private static (double Red, double Green, double Blue) HsvToRgb(double hue, double saturation, double value)
    {
        var sector = hue * 6;
        var index = (int)Math.Floor(sector);
        var fraction = sector - index;
        var p = value * (1 - saturation);
        var q = value * (1 - (fraction * saturation));
        var t = value * (1 - ((1 - fraction) * saturation));
        var (red, green, blue) = index % 6 switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };
        return (red * 255, green * 255, blue * 255);
    }

    private sealed record CursorCacheEntry(MemoryStream Stream, Cursor Cursor);
}
