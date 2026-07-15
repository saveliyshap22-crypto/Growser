using System.Net;
using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public static class NewTabPageService
{
    public static string Build(AppSettings settings, IReadOnlyCollection<BookmarkEntry> bookmarks, bool isPrivate)
    {
        var wallpaper = BuildWallpaper(settings.NewTabWallpaperPath);
        var accent = NormalizeAccent(settings.AccentColor);
        var motionClass = settings.ReduceMotion ? " reduce-motion" : string.Empty;
        var glowClass = settings.ShowAmbientGlow ? string.Empty : " no-glow";
        var privateBadge = isPrivate
            ? "<div class=\"private\">Инкогнито · данные сеанса будут удалены</div>"
            : string.Empty;
        var subtitle = isPrivate
            ? "История, cookies и данные этой приватной сессии не сохраняются."
            : "Быстрый поиск, избранные сайты и конструктор расширенных запросов.";
        var bookmarkCards = isPrivate ? string.Empty : BuildBookmarkCards(bookmarks);
        var (searchAction, queryName) = ResolveSearchForm(settings.SearchUrlTemplate);

        return $$$"""
            <!doctype html>
            <html lang="ru">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'; form-action https:; navigate-to https: http:">
              <title>Новая вкладка</title>
              <style>
                :root{color-scheme:dark;font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;--accent:{{{accent}}};--accent-soft:color-mix(in srgb,var(--accent) 24%,transparent);--accent-mid:color-mix(in srgb,var(--accent) 58%,transparent)}
                *{box-sizing:border-box}html{min-width:300px;background:#0b0c0f}
                body{margin:0;min-height:100vh;overflow-x:hidden;color:#f8f9fb;background:{{{wallpaper}}}}
                body::before{content:'';position:fixed;inset:-130px;pointer-events:none;background:radial-gradient(circle at 16% 10%,var(--accent-soft),transparent 31%),radial-gradient(circle at 84% 78%,color-mix(in srgb,var(--accent) 14%,transparent),transparent 39%);filter:blur(28px);animation:floatGlow 12s ease-in-out infinite alternate}
                body::after{content:'';position:fixed;inset:0;pointer-events:none;opacity:.11;background-image:linear-gradient(#fff1 1px,transparent 1px),linear-gradient(90deg,#fff1 1px,transparent 1px);background-size:48px 48px;mask-image:linear-gradient(to bottom,black,transparent 80%)}
                body.no-glow::before,body.no-glow::after{display:none}
                main{position:relative;z-index:1;min-height:100vh;width:min(1080px,calc(100vw - 34px));margin:0 auto;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:48px 0 68px}
                .topline{width:min(920px,100%);display:flex;align-items:center;justify-content:space-between;gap:16px;margin-bottom:14px;color:#aeb3bd;font-size:13px}.clock{font-variant-numeric:tabular-nums;color:color-mix(in srgb,var(--accent) 76%,white);font-weight:700}.private{padding:9px 15px;border:1px solid var(--accent-mid);border-radius:999px;background:#17191ed9;backdrop-filter:blur(26px);color:#e2e4e9;font-size:13px}
                .brand{margin:0;text-align:center;font-size:clamp(42px,6.4vw,74px);font-weight:780;letter-spacing:clamp(-4px,-.055em,-2px);line-height:1;text-shadow:0 18px 52px #000b;animation:brandIn .55s cubic-bezier(.2,.8,.2,1) both}.brand span{color:var(--accent)}
                .tagline{max-width:720px;margin:13px 18px 23px;color:#adb1bb;text-align:center;font-size:14px;line-height:1.5;text-wrap:balance;animation:fadeUp .48s .06s both}
                .switcher{display:flex;max-width:100%;gap:5px;margin:0 0 18px;padding:5px;border:1px solid #ffffff24;border-radius:999px;background:#14161bc9;backdrop-filter:blur(26px);animation:fadeUp .48s .11s both}
                .switcher button{min-width:min(136px,40vw);height:41px;padding:0 18px;border:0;border-radius:999px;background:transparent;color:#aeb2bc;font:650 14px inherit;cursor:pointer;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;transition:.18s ease}.switcher button.active{color:#17181b;background:var(--accent);box-shadow:0 8px 28px var(--accent-soft),inset 0 1px 0 #fff8}
                .panel{width:min(900px,100%);animation:panelIn .34s cubic-bezier(.2,.8,.2,1) both}.hidden{display:none!important}
                .search-form{height:72px;display:flex;align-items:center;gap:12px;padding:0 12px 0 24px;border:1px solid #ffffff3a;border-radius:999px;background:linear-gradient(145deg,#ffffff20,#ffffff0b);box-shadow:0 28px 82px #0009,inset 0 1px 0 #ffffff3f;backdrop-filter:blur(34px) saturate(1.42);transition:.18s ease}.search-form:focus-within{transform:translateY(-2px) scale(1.003);border-color:color-mix(in srgb,var(--accent) 76%,white);box-shadow:0 34px 96px #000a,0 0 0 4px var(--accent-soft)}
                .search-icon{flex:0 0 auto;color:var(--accent);font-size:23px}input,select,textarea{color:#fff;font:500 15px inherit}input::placeholder,textarea::placeholder{color:#858a95}.search-form input{min-width:0;flex:1;border:0;outline:0;background:transparent;font-size:18px;text-overflow:ellipsis}
                .round-button{width:49px;height:49px;flex:0 0 49px;display:grid;place-items:center;border:0;border-radius:999px;background:var(--accent);color:#17181b;font-size:22px;font-weight:850;cursor:pointer;transition:.16s ease}.round-button:hover,.action:hover,.bookmark:hover{transform:translateY(-2px);filter:brightness(1.06)}.round-button:active,.action:active{transform:scale(.97)}
                .bookmarks{width:min(900px,100%);display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:10px;margin-top:14px}.bookmark{min-width:0;display:flex;align-items:center;gap:10px;padding:11px 13px;border:1px solid #ffffff22;border-radius:17px;background:linear-gradient(145deg,#ffffff15,#ffffff08);color:#e7e9ed;text-decoration:none;backdrop-filter:blur(24px);transition:.17s ease}.bookmark:hover{border-color:var(--accent-mid)}.bookmark-icon{width:30px;height:30px;flex:0 0 30px;display:grid;place-items:center;border-radius:11px;background:var(--accent-soft);color:var(--accent);font-weight:800}.bookmark-text{min-width:0;display:flex;flex-direction:column}.bookmark-title,.bookmark-host{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.bookmark-title{font-size:13px;font-weight:650}.bookmark-host{margin-top:2px;color:#8f949e;font-size:10px}
                .studio{padding:22px;border:1px solid #ffffff2f;border-radius:31px;background:linear-gradient(150deg,#22242ae8,#101217ed);box-shadow:0 34px 100px #000a,inset 0 1px 0 #ffffff25;backdrop-filter:blur(34px) saturate(1.32)}.studio-head{display:flex;align-items:flex-start;justify-content:space-between;gap:16px;margin-bottom:18px}.studio-title{margin:0;font-size:clamp(19px,3vw,23px);overflow-wrap:anywhere}.studio-note{max-width:670px;margin:6px 0 0;color:#9ca1ac;font-size:13px;line-height:1.45}.badge{flex:0 0 auto;padding:7px 11px;border-radius:999px;border:1px solid var(--accent-mid);background:var(--accent-soft);color:color-mix(in srgb,var(--accent) 52%,white);font-size:12px;white-space:nowrap}
                .grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.field{min-width:0;display:flex;flex-direction:column;gap:7px}.field.wide{grid-column:1/-1}label{color:#c9ccd2;font-size:12px;font-weight:650;overflow-wrap:anywhere}.control{width:100%;min-width:0;min-height:46px;border:1px solid #ffffff24;border-radius:16px;outline:0;background:#0e1015d6;padding:11px 13px;transition:.16s ease}.control:focus{border-color:var(--accent);box-shadow:0 0 0 3px var(--accent-soft)}
                .options{display:flex;flex-wrap:wrap;gap:9px;margin:14px 0}.check{max-width:100%;display:flex;align-items:center;gap:8px;padding:9px 12px;border:1px solid #ffffff20;border-radius:14px;background:#ffffff08;color:#c4c7ce;font-size:13px}.check input{accent-color:var(--accent)}.preview{min-height:72px;margin-top:6px;padding:14px 16px;border:1px solid #ffffff24;border-radius:17px;background:#090b0f;color:color-mix(in srgb,var(--accent) 48%,white);font:500 14px/1.5 'Cascadia Code','Consolas',monospace;overflow-wrap:anywhere;white-space:pre-wrap}
                .actions{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:9px;margin-top:13px}.action{min-width:0;min-height:43px;padding:8px 12px;border:1px solid #ffffff25;border-radius:14px;background:#ffffff0d;color:#f2f3f5;font:650 13px inherit;cursor:pointer;white-space:normal;overflow-wrap:anywhere;transition:.16s ease}.action.primary{border-color:transparent;background:var(--accent);color:#17181b}.subtitle{max-width:720px;margin:17px 24px 0;text-align:center;color:#979ca6;font-size:12px;line-height:1.5}
                .reduce-motion *{animation:none!important;transition:none!important}@keyframes brandIn{from{opacity:0;transform:translateY(12px) scale(.97)}to{opacity:1;transform:none}}@keyframes fadeUp{from{opacity:0;transform:translateY(9px)}to{opacity:1;transform:none}}@keyframes panelIn{from{opacity:0;transform:translateY(8px) scale(.992)}to{opacity:1;transform:none}}@keyframes floatGlow{from{transform:translate(-1.5%,-1%) scale(1)}to{transform:translate(1.5%,1%) scale(1.035)}}
                @media(max-width:720px){main{width:min(100% - 24px,1080px);padding:34px 0 48px}.grid{grid-template-columns:1fr}.field.wide{grid-column:auto}.studio{padding:17px;border-radius:25px}.studio-head{flex-direction:column}.search-form{height:64px;padding-left:18px}.actions{grid-template-columns:1fr 1fr}.bookmarks{grid-template-columns:1fr 1fr}}
                @media(max-width:430px){.topline{justify-content:center}.clock{display:none}.switcher{width:100%}.switcher button{min-width:0;flex:1;padding:0 10px}.actions,.bookmarks{grid-template-columns:1fr}.search-icon{display:none}.studio{padding:14px}.check{width:100%}}
              </style>
            </head>
            <body class="{{{motionClass}}}{{{glowClass}}}">
              <main>
                <div class="topline"><div>{{{privateBadge}}}</div><time id="clock" class="clock"></time></div>
                <h1 class="brand">Chroma <span>Browser</span></h1>
                <p class="tagline">{{{WebUtility.HtmlEncode(subtitle)}}}</p>
                <nav class="switcher"><button id="searchTab" class="active" type="button">Поиск</button><button id="dorkTab" type="button">Dork Studio</button></nav>
                <section id="searchPanel" class="panel">
                  <form class="search-form" action="{{{WebUtility.HtmlEncode(searchAction)}}}" method="get"><span class="search-icon">⌕</span><input name="{{{WebUtility.HtmlEncode(queryName)}}}" autofocus autocomplete="off" enterkeyhint="search" placeholder="Поиск в интернете"><button class="round-button" type="submit" aria-label="Найти">→</button></form>
                  {{{bookmarkCards}}}
                </section>
                <section id="dorkPanel" class="panel hidden">
                  <div class="studio">
                    <div class="studio-head"><div><h2 class="studio-title">Конструктор поисковых запросов</h2><p class="studio-note">Формирует стандартные операторы публичного веб-поиска. Используйте только для законного поиска открытой информации.</p></div><span class="badge">Public search</span></div>
                    <div class="grid">
                      <div class="field wide"><label for="target">Что ищем</label><input id="target" class="control" autocomplete="off" placeholder="Ник, номер, фраза или название"></div>
                      <div class="field"><label for="kind">Тип поиска</label><select id="kind" class="control"><option value="all">Обычный запрос</option><option value="nickname">Никнейм</option><option value="phone">Номер телефона</option><option value="site">Сайт или домен</option><option value="title">В заголовке страницы</option><option value="url">В адресе страницы</option></select></div>
                      <div class="field"><label for="filetype">Формат результата</label><select id="filetype" class="control"><option value="">Все форматы</option><option value="pdf">PDF</option><option value="docx">DOCX</option><option value="xlsx">XLSX</option><option value="pptx">PPTX</option><option value="txt">TXT</option></select></div>
                      <div class="field"><label for="domain">Искать только на сайте</label><input id="domain" class="control" placeholder="example.com — необязательно"></div>
                      <div class="field"><label for="extra">Дополнительные слова</label><input id="extra" class="control" placeholder="город, тема, организация"></div>
                      <div class="field wide"><label for="exclude">Исключить слова</label><input id="exclude" class="control" placeholder="реклама магазин — необязательно"></div>
                    </div>
                    <div class="options"><label class="check"><input id="exact" type="checkbox" checked> Точное совпадение</label><label class="check"><input id="documents" type="checkbox"> Только страницы с документами</label></div>
                    <div id="preview" class="preview">Введите данные — здесь появится готовый запрос.</div>
                    <div class="actions"><button id="copy" class="action" type="button">Копировать</button><button data-engine="google" class="action primary" type="button">Искать в Google</button><button data-engine="bing" class="action" type="button">Bing</button><button data-engine="duck" class="action" type="button">DuckDuckGo</button></div>
                  </div>
                </section>
                <p class="subtitle">Dork Studio не сканирует сайты и не обходит защиту — он только собирает запрос для выбранной поисковой системы.</p>
              </main>
              <script>
                const clock=document.getElementById('clock');function updateClock(){clock.textContent=new Intl.DateTimeFormat('ru-RU',{weekday:'short',hour:'2-digit',minute:'2-digit'}).format(new Date())}updateClock();setInterval(updateClock,30000);
                const searchTab=document.getElementById('searchTab'),dorkTab=document.getElementById('dorkTab'),searchPanel=document.getElementById('searchPanel'),dorkPanel=document.getElementById('dorkPanel'),preview=document.getElementById('preview');let currentQuery='';
                function setMode(mode){const dork=mode==='dork';searchTab.classList.toggle('active',!dork);dorkTab.classList.toggle('active',dork);searchPanel.classList.toggle('hidden',dork);dorkPanel.classList.toggle('hidden',!dork);if(dork)document.getElementById('target').focus()}searchTab.onclick=()=>setMode('search');dorkTab.onclick=()=>setMode('dork');
                function clean(v){return v.trim().replace(/\s+/g,' ')}function quote(v){return `"${v.replaceAll('"','').trim()}"`}function domain(v){return clean(v).replace(/^https?:\/\//i,'').replace(/^www\./i,'').split('/')[0]}
                function build(){const target=clean(document.getElementById('target').value),kind=document.getElementById('kind').value,exact=document.getElementById('exact').checked,type=document.getElementById('filetype').value,onlyDomain=domain(document.getElementById('domain').value),extra=clean(document.getElementById('extra').value),excluded=clean(document.getElementById('exclude').value).split(' ').filter(Boolean),docs=document.getElementById('documents').checked,parts=[];if(target){const value=exact?quote(target):target;if(kind==='nickname'){const nick=target.replace(/^@/,'');parts.push(`(${quote(nick)} OR ${quote('@'+nick)})`)}else if(kind==='phone'){const digits=target.replace(/\D/g,'');parts.push(digits.length>=7&&digits!==target?`(${quote(target)} OR ${quote(digits)})`:value)}else if(kind==='site'){const host=domain(target);if(host)parts.push(`site:${host}`)}else if(kind==='title')parts.push(`intitle:${value}`);else if(kind==='url')parts.push(`inurl:${value}`);else parts.push(value)}if(onlyDomain&&kind!=='site')parts.push(`site:${onlyDomain}`);if(extra)parts.push(exact?quote(extra):extra);if(type)parts.push(`filetype:${type}`);else if(docs)parts.push('(filetype:pdf OR filetype:docx OR filetype:xlsx OR filetype:pptx)');excluded.forEach(w=>parts.push(`-${w.replace(/^-/,'')}`));currentQuery=parts.join(' ').trim();preview.textContent=currentQuery||'Введите данные — здесь появится готовый запрос.'}
                ['target','kind','filetype','domain','extra','exclude','exact','documents'].forEach(id=>{const e=document.getElementById(id);e.oninput=build;e.onchange=build});
                document.getElementById('copy').onclick=async()=>{if(!currentQuery)return;try{await navigator.clipboard.writeText(currentQuery)}catch{const a=document.createElement('textarea');a.value=currentQuery;document.body.appendChild(a);a.select();document.execCommand('copy');a.remove()}const b=document.getElementById('copy');b.textContent='Скопировано';setTimeout(()=>b.textContent='Копировать',1200)};
                document.querySelectorAll('[data-engine]').forEach(b=>b.onclick=()=>{if(!currentQuery)return;const q=encodeURIComponent(currentQuery),urls={google:`https://www.google.com/search?q=${q}`,bing:`https://www.bing.com/search?q=${q}`,duck:`https://duckduckgo.com/?q=${q}`};window.open(urls[b.dataset.engine],'_blank','noopener')});
              </script>
            </body>
            </html>
            """;
    }

    private static string BuildBookmarkCards(IEnumerable<BookmarkEntry> bookmarks)
    {
        var cards = bookmarks
            .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .OrderByDescending(item => item.CreatedAt)
            .Take(8)
            .Select(item =>
            {
                var uri = new Uri(item.Url);
                var title = string.IsNullOrWhiteSpace(item.Title) ? uri.Host : item.Title.Trim();
                var initial = title.FirstOrDefault(char.IsLetterOrDigit);
                var icon = initial == default ? "•" : char.ToUpperInvariant(initial).ToString();
                return $"<a class=\"bookmark\" href=\"{WebUtility.HtmlEncode(uri.ToString())}\"><span class=\"bookmark-icon\">{WebUtility.HtmlEncode(icon)}</span><span class=\"bookmark-text\"><span class=\"bookmark-title\">{WebUtility.HtmlEncode(title)}</span><span class=\"bookmark-host\">{WebUtility.HtmlEncode(uri.Host)}</span></span></a>";
            })
            .ToArray();
        return cards.Length == 0 ? string.Empty : $"<div class=\"bookmarks\">{string.Join(string.Empty, cards)}</div>";
    }

    private static (string Action, string QueryName) ResolveSearchForm(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) return ("https://www.google.com/search", "q");
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

    private static string NormalizeAccent(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : "#FF8A3D";

    private static string BuildWallpaper(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length <= 15 * 1024 * 1024)
            {
                var mediaType = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
                return $"linear-gradient(#08090d75,#08090dd2),url(data:{mediaType};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}) center/cover fixed";
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Wallpaper could not be loaded: {exception.Message}");
        }
        return "linear-gradient(145deg,#090a0d,#202228 52%,#0c0d10)";
    }
}
