using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public static class NavigationService
{
    public const string NewTab = "chroma://newtab";

    public static string Resolve(string input, AppSettings settings)
    {
        input = input.Trim();
        if (string.IsNullOrWhiteSpace(input) || input.Equals(NewTab, StringComparison.OrdinalIgnoreCase))
        {
            return NewTab;
        }

        var command = TryResolveCommand(input);
        if (command is not null)
        {
            return command;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute) && IsAllowedScheme(absolute.Scheme))
        {
            return absolute.ToString();
        }

        if (LooksLikeHost(input))
        {
            var scheme = settings.HttpsFirst ? "https://" : "http://";
            if (Uri.TryCreate(scheme + input, UriKind.Absolute, out var hostUri))
            {
                return hostUri.ToString();
            }
        }

        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            settings.SearchUrlTemplate,
            Uri.EscapeDataString(input));
    }

    public static string Display(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || source == "about:blank")
        {
            return NewTab;
        }

        return source;
    }

    private static bool IsAllowedScheme(string scheme) => scheme is "http" or "https" or "file" or "data";

    private static bool LooksLikeHost(string value)
    {
        if (value.Contains(' ') || value.Contains('\n') || value.Contains('\r'))
        {
            return false;
        }

        var hostPart = value.Split('/')[0].Split(':')[0];
        return hostPart.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               hostPart.Contains('.') ||
               System.Net.IPAddress.TryParse(hostPart, out _);
    }

    private static string? TryResolveCommand(string input)
    {
        var split = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !split[0].StartsWith('!'))
        {
            return null;
        }

        var query = Uri.EscapeDataString(split[1]);
        return split[0].ToLowerInvariant() switch
        {
            "!g" => $"https://www.google.com/search?q={query}",
            "!ddg" => $"https://duckduckgo.com/?q={query}",
            "!b" => $"https://www.bing.com/search?q={query}",
            "!w" => $"https://ru.wikipedia.org/w/index.php?search={query}",
            "!yt" => $"https://www.youtube.com/results?search_query={query}",
            "!ai" => $"https://chat.mistral.ai/chat?q={query}",
            _ => null
        };
    }
}
