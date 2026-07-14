using System.Net;
using System.Text;
using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public static class NewTabPageService
{
    public static string Build(AppSettings settings, IReadOnlyCollection<BookmarkEntry> bookmarks, bool isPrivate)
    {
        var wallpaper = BuildWallpaper(settings.NewTabWallpaperPath);
        var shortcuts = string.Join(
            Environment.NewLine,
            bookmarks.Take(8).Select(bookmark => $$"""
                <a class="shortcut" href="{{WebUtility.HtmlEncode(bookmark.Url)}}">
                  <span class="shortcut-icon">{{WebUtility.HtmlEncode(GetInitial(bookmark.Title))}}</span>
                  <span>{{WebUtility.HtmlEncode(Trim(bookmark.Title, 22))}}</span>
                </a>
                """));

        var privateBadge = isPrivate ? "<div class=\"private\">Инкогнито · данные сеанса будут удалены</div>" : string.Empty;
        var queryIndex = settings.SearchUrlTemplate.IndexOf('?', StringComparison.Ordinal);
        var searchAction = queryIndex >= 0 ? settings.SearchUrlTemplate[..queryIndex] : settings.SearchUrlTemplate;
        var searchUrl = WebUtility.HtmlEncode(searchAction.Replace("{0}", string.Empty, StringComparison.Ordinal));

        return $$"""
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
                body { margin:0; min-height:100vh; overflow:hidden; color:#f6f7fb; background:{{wallpaper}}; }
                body::before { content:''; position:fixed; inset:-80px; background:radial-gradient(circle at 18% 20%,#8b98ff32,transparent 30%),radial-gradient(circle at 80% 70%,#d2d6ff1f,transparent 38%); filter:blur(12px); }
                main { position:relative; z-index:1; width:min(820px,calc(100vw - 48px)); margin:0 auto; padding-top:clamp(90px,16vh,180px); }
                .private { width:max-content; max-width:100%; margin:0 auto 24px; padding:9px 14px; border:1px solid #ffffff30; border-radius:999px; background:#20232bb3; backdrop-filter:blur(24px); color:#cbd0da; font-size:13px; }
                h1 { margin:0 0 28px; text-align:center; font-size:clamp(38px,6vw,66px); font-weight:680; letter-spacing:-3px; text-shadow:0 12px 38px #0009; }
                h1 span { color:#c3caff; }
                form { display:flex; align-items:center; height:62px; padding:0 18px; border:1px solid #ffffff35; border-radius:22px; background:linear-gradient(145deg,#ffffff20,#ffffff0e); box-shadow:0 24px 70px #0008,inset 0 1px 0 #ffffff38; backdrop-filter:blur(28px) saturate(1.35); }
                input { width:100%; border:0; outline:0; color:#fff; background:transparent; font:500 17px inherit; }
                input::placeholder { color:#b8bdc8; }
                button { border:0; border-radius:14px; padding:10px 16px; background:#d5d9ff; color:#181a20; font-weight:700; cursor:pointer; }
                .shortcuts { display:grid; grid-template-columns:repeat(4,minmax(0,1fr)); gap:14px; margin-top:28px; }
                .shortcut { display:flex; align-items:center; gap:11px; min-width:0; padding:13px; color:#eef0f6; text-decoration:none; border:1px solid #ffffff20; border-radius:17px; background:#1e2026a8; box-shadow:inset 0 1px 0 #ffffff18; backdrop-filter:blur(22px); transition:.18s ease; }
                .shortcut:hover { transform:translateY(-3px); background:#30343fcf; border-color:#ffffff42; }
                .shortcut-icon { display:grid; place-items:center; flex:0 0 36px; height:36px; border-radius:12px; background:linear-gradient(145deg,#dfe2ff,#8089bd); color:#181a22; font-weight:800; }
                .shortcut span:last-child { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
                footer { margin-top:28px; text-align:center; color:#a9aeba; font-size:12px; }
                @media(max-width:620px){.shortcuts{grid-template-columns:repeat(2,1fr)} h1{letter-spacing:-2px}}
              </style>
            </head>
            <body>
              <main>
                {{privateBadge}}
                <h1>Chroma <span>Browser</span></h1>
                <form action="{{searchUrl}}" method="get" onsubmit="this.action=this.action||'https://www.google.com/search';">
                  <input name="q" autofocus autocomplete="off" placeholder="Поиск или адрес">
                  <button type="submit">Найти</button>
                </form>
                <section class="shortcuts">{{shortcuts}}</section>
                <footer>Быстрые команды: !g · !ddg · !w · !yt · !ai</footer>
              </main>
            </body>
            </html>
            """;
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
                return $"linear-gradient(#080a0f70,#080a0fb8),url(data:{mediaType};base64,{data}) center/cover fixed";
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Wallpaper could not be loaded: {exception.Message}");
        }

        return "linear-gradient(145deg,#111319,#242832 52%,#13151b)";
    }

    private static string GetInitial(string title) =>
        string.IsNullOrWhiteSpace(title) ? "•" : char.ToUpperInvariant(title.Trim()[0]).ToString();

    private static string Trim(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";
}
