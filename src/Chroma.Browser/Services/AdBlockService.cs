using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Chroma.Browser.Models;
using Microsoft.Web.WebView2.Core;

namespace Chroma.Browser.Services;

public sealed class AdBlockService
{
    private static readonly HttpClient Client = CreateClient();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowedDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _threatDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _cosmeticSelectors = new(StringComparer.Ordinal);
    private int _ruleCount;

    public int RuleCount => Volatile.Read(ref _ruleCount);
    public DateTimeOffset? LastUpdated { get; private set; }
    public event EventHandler? RulesChanged;

    public async Task InitializeAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        LoadFromDisk(settings);

        var newestFile = Directory.EnumerateFiles(AppPaths.Filters, "*.txt")
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        if (newestFile < DateTime.UtcNow.AddDays(-7))
        {
            await UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        foreach (var subscription in settings.FilterSubscriptions.Where(item => item.Enabled))
        {
            try
            {
                using var response = await Client.GetAsync(subscription.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var filePath = GetSubscriptionPath(subscription);
                var temporaryPath = filePath + ".tmp";
                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                await using (var destination = File.Create(temporaryPath))
                {
                    await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                }

                File.Move(temporaryPath, filePath, true);
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
            {
                LogService.Instance.Warn($"Filter update failed for {subscription.Name}: {exception.Message}");
            }
        }

        LoadFromDisk(settings);
        LastUpdated = DateTimeOffset.Now;
        RulesChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool ShouldBlock(
        Uri request,
        Uri? document,
        CoreWebView2WebResourceContext context,
        out string reason)
    {
        reason = string.Empty;
        if (request.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var host = request.IdnHost.TrimEnd('.');
        _lock.EnterReadLock();
        try
        {
            if (IsListed(host, _allowedDomains))
            {
                return false;
            }

            if (IsListed(host, _threatDomains))
            {
                reason = "Вредоносный домен";
                return true;
            }

            if (context == CoreWebView2WebResourceContext.Document)
            {
                return false;
            }

            if (IsListed(host, _blockedDomains))
            {
                reason = document is not null && IsThirdParty(host, document.IdnHost)
                    ? "Сторонний трекер или реклама"
                    : "Рекламный ресурс";
                return true;
            }

            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string GetCosmeticScript()
    {
        _lock.EnterReadLock();
        try
        {
            var selectors = _cosmeticSelectors.Take(1200).ToArray();
            if (selectors.Length == 0)
            {
                selectors =
                [
                    "[id^='google_ads_']", ".adsbygoogle", "[class*=' ad-banner']",
                    "[class^='ad-container']", "[data-ad-slot]", "[aria-label='Advertisement']"
                ];
            }

            var css = string.Join(',', selectors);
            var escapedCss = System.Text.Json.JsonSerializer.Serialize(css);
            return $$"""
                (() => {
                  const apply = () => {
                    if (document.getElementById('chroma-cosmetic-rules')) return;
                    const style = document.createElement('style');
                    style.id = 'chroma-cosmetic-rules';
                    style.textContent = {{escapedCss}} + '{display:none!important;visibility:hidden!important}';
                    (document.head || document.documentElement).appendChild(style);
                  };
                  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', apply, {once:true});
                  else apply();
                })();
                """;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void LoadFromDisk(AppSettings settings)
    {
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var threats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectors = new HashSet<string>(StringComparer.Ordinal);

        foreach (var subscription in settings.FilterSubscriptions.Where(item => item.Enabled))
        {
            var path = GetSubscriptionPath(subscription);
            if (!File.Exists(path))
            {
                continue;
            }

            var target = subscription.Name.Contains("URLhaus", StringComparison.OrdinalIgnoreCase) ? threats : blocked;
            ParseFile(path, target, allowed, selectors);
        }

        _lock.EnterWriteLock();
        try
        {
            Replace(_blockedDomains, blocked);
            Replace(_allowedDomains, allowed);
            Replace(_threatDomains, threats);
            Replace(_cosmeticSelectors, selectors);
            Volatile.Write(ref _ruleCount, blocked.Count + allowed.Count + threats.Count + selectors.Count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static void ParseFile(
        string path,
        HashSet<string> blocked,
        HashSet<string> allowed,
        HashSet<string> selectors)
    {
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length < 3 || line[0] is '!' or '[' or '#')
            {
                continue;
            }

            var cosmeticIndex = line.IndexOf("##", StringComparison.Ordinal);
            if (cosmeticIndex >= 0 && cosmeticIndex + 2 < line.Length)
            {
                var selector = line[(cosmeticIndex + 2)..];
                if (IsSafeCosmeticSelector(selector))
                {
                    selectors.Add(selector);
                }

                continue;
            }

            var hostLine = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (hostLine.Length >= 2 && IPAddress.TryParse(hostLine[0], out _))
            {
                AddDomain(hostLine[1], blocked);
                continue;
            }

            var isException = line.StartsWith("@@", StringComparison.Ordinal);
            if (isException)
            {
                line = line[2..];
            }

            if (!line.StartsWith("||", StringComparison.Ordinal))
            {
                continue;
            }

            line = line[2..];
            var end = line.IndexOfAny(['^', '/', '$', '*']);
            var domain = end < 0 ? line : line[..end];
            AddDomain(domain, isException ? allowed : blocked);
        }
    }

    private static void AddDomain(string value, HashSet<string> destination)
    {
        value = value.Trim().TrimEnd('.').ToLowerInvariant();
        if (value.StartsWith("www.", StringComparison.Ordinal))
        {
            value = value[4..];
        }

        if (value.Length is < 4 or > 253 || value.Contains('*') || value.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-')))
        {
            return;
        }

        destination.Add(value);
    }

    private static bool IsSafeCosmeticSelector(string selector) =>
        selector.Length is > 1 and < 300 &&
        !selector.Contains('{') &&
        !selector.Contains('}') &&
        !selector.Contains("url(", StringComparison.OrdinalIgnoreCase) &&
        !selector.Contains(":has(", StringComparison.OrdinalIgnoreCase);

    private static bool IsListed(string host, HashSet<string> domains)
    {
        var candidate = host;
        while (true)
        {
            if (domains.Contains(candidate))
            {
                return true;
            }

            var dot = candidate.IndexOf('.');
            if (dot < 0)
            {
                return false;
            }

            candidate = candidate[(dot + 1)..];
        }
    }

    private static bool IsThirdParty(string requestHost, string documentHost) =>
        !requestHost.Equals(documentHost, StringComparison.OrdinalIgnoreCase) &&
        !requestHost.EndsWith('.' + documentHost, StringComparison.OrdinalIgnoreCase) &&
        !documentHost.EndsWith('.' + requestHost, StringComparison.OrdinalIgnoreCase);

    private static void Replace(HashSet<string> destination, HashSet<string> source)
    {
        destination.Clear();
        destination.UnionWith(source);
    }

    private static string GetSubscriptionPath(FilterSubscription subscription)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subscription.Url)))[..16];
        return Path.Combine(AppPaths.Filters, $"{hash}.txt");
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ChromaBrowser/0.1 (+https://github.com/saveliyshap22-crypto/Growser)");
        return client;
    }
}
