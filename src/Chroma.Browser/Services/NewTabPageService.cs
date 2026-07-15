using System.Net;
using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public static class NewTabPageService
{
    public static string Build(AppSettings settings, IReadOnlyCollection<BookmarkEntry> bookmarks, bool isPrivate)
    {
        _ = bookmarks;
        var wallpaper = BuildWallpaper(settings.NewTabWallpaperPath);
        var privateBadge = isPrivate
            ? "<div class=\"private\">Инкогнито · данные сеанса будут удалены</div>"
            : string.Empty;
        var subtitle = isPrivate
            ? "История, cookies и данные этой приватной сессии не сохраняются."
            : "Введите запрос — результаты откроются в выбранной поисковой системе.";
        var (searchAction, queryName) = ResolveSearchForm(settings.SearchUrlTemplate);

        return $$$"""
            <!doctype html>
            <html lang="ru">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; form-action https:; navigate-to https: http:">
              <title>Новая вкладка</title>
              <style>
                :root { color-scheme: dark; font-family: 'Segoe UI Variable Text','Segoe UI',sans-serif; }
                * { box-sizing: border-box; }
                body { margin:0; min-height:100vh; overflow:hidden; color:#f7f8fb; background:{{{wallpaper}}}; }
                body::before { content:''; position:fixed; inset:-90px; pointer-events:none; background:radial-gradient(circle at 18% 18%,#aeb8ff35,transparent 31%),radial-gradient(circle at 82% 72%,#d7dbff22,transparent 38%); filter:blur(18px); }
                main { position:relative; z-index:1; min-height:100vh; width:min(920px,calc(100vw - 44px)); margin:0 auto; display:flex; flex-direction:column; align-items:center; justify-content:center; padding:44px 0 96px; }
                .private { margin:0 0 24px; padding:9px 15px; border:1px solid #ffffff2d; border-radius:999px; background:#20232bc7; backdrop-filter:blur(24px); color:#d3d7e0; font-size:13px; box-shadow:inset 0 1px 0 #ffffff16; }
                .brand { margin:0 0 28px; text-align:center; font-size:clamp(44px,7vw,76px); font-weight:720; letter-spacing:-4px; line-height:1; text-shadow:0 16px 42px #000a; }
                .brand span { color:#cbd1ff; }
                form { width:min(760px,100%); height:70px; display:flex; align-items:center; gap:12px; padding:0 12px 0 24px; border:1px solid #ffffff3d; border-radius:999px; background:linear-gradient(145deg,#ffffff25,#ffffff0e); box-shadow:0 28px 82px #0009,inset 0 1px 0 #ffffff42; backdrop-filter:blur(30px) saturate(1.4); transition:transform .18s ease,border-color .18s ease,box-shadow .18s ease; }
                form:focus-within { transform:translateY(-2px); border-color:#cbd1ff99; box-shadow:0 34px 92px #000a,0 0 0 4px #aeb8ff18,inset 0 1px 0 #ffffff55; }
                .search-icon { flex:0 0 auto; color:#c7ccd6; font-size:22px; line-height:1; }
                input { min-width:0; flex:1; border:0; outline:0; color:#fff; background:transparent; font:500 18px inherit; }
                input::placeholder { color:#b9bec8; }
                button { width:48px; height:48px; flex:0 0 48px; display:grid; place-items:center; border:0; border-radius:999px; background:#d9ddff; color:#17191f; font-size:22px; font-weight:800; cursor:pointer; box-shadow:inset 0 1px 0 #fff8; transition:transform .16s ease,filter .16s ease; }
                button:hover { transform:scale(1.04); filter:brightness(1.05); }
                button:active { transform:scale(.96); }
                .subtitle { max-width:620px; margin:18px 24px 0; text-align:center; color:#adb3be; font-size:14px; line-height:1.5; }
                @media(max-width:620px){main{padding-bottom:68px}.brand{letter-spacing:-2.5px}form{height:64px;padding-left:18px}input{font-size:16px}.subtitle{font-size:13px}}
              </style>
            </head>
            <body>
              <main>
                {{{privateBadge}}}
                <h1 class="brand">Chroma <span>Browser</span></h1>
                <form action="{{{WebUtility.HtmlEncode(searchAction)}}}" method="get">
                  <span class="search-icon" aria-hidden="true">⌕</span>
                  <input name="{{{WebUtility.HtmlEncode(queryName)}}}" autofocus autocomplete="off" enterkeyhint="search" placeholder="Поиск в интернете">
                  <button type="submit" aria-label="Найти">→</button>
                </form>
                <p class="subtitle">{{{WebUtility.HtmlEncode(subtitle)}}}</p>
              </main>
            </body>
            </html>
            """;
    }

    private static (string Action, string QueryName) ResolveSearchForm(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return ("https://www.google.com/search", "q");
        }

        var markerIndex = template.IndexOf("{0}", StringComparison.Ordinal);
        var prefix = markerIndex >= 0 ? template[..markerIndex] : template;
        var questionIndex = prefix.IndexOf('?', StringComparison.Ordinal);
        var action = questionIndex >= 0 ? prefix[..questionIndex] : prefix;
        var parameterStart = prefix.LastIndexOfAny(['?', '&']);
        var parameter = parameterStart >= 0 ? prefix[(parameterStart + 1)..] : string.Empty;
        var equalsIndex = parameter.IndexOf('=', StringComparison.Ordinal);
        var queryName = equalsIndex > 0 ? parameter[..equalsIndex] : "q";

        return (string.IsNullOrWhiteSpace(action) ? "https://www.google.com/search" : action, queryName);
    }

    private static string BuildWallpaper(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length <= 15 * 1024 * 1024)
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                var mediaType = extension switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };
                var data = Convert.ToBase64String(File.ReadAllBytes(path));
                return $"linear-gradient(#080a0f70,#080a0fbd),url(data:{mediaType};base64,{data}) center/cover fixed";
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Wallpaper could not be loaded: {exception.Message}");
        }

        return "linear-gradient(145deg,#101218,#252a35 52%,#111318)";
    }
}
