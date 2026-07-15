using System.Net;
using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public static class NewTabPageService
{
    public static string Build(AppSettings settings, IReadOnlyCollection<BookmarkEntry> bookmarks, bool isPrivate)
    {
        _ = bookmarks;
        var wallpaper = BuildWallpaper(settings.NewTabWallpaperPath);
        var accent = NormalizeAccent(settings.AccentColor);
        var privateBadge = isPrivate
            ? "<div class=\"private\">Инкогнито · данные сеанса будут удалены</div>"
            : string.Empty;
        var subtitle = isPrivate
            ? "История, cookies и данные этой приватной сессии не сохраняются."
            : "Быстрый поиск или конструктор расширенных поисковых запросов.";
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
                :root { color-scheme:dark; font-family:'Segoe UI Variable Text','Segoe UI',sans-serif; --accent:{{{accent}}}; --accent-soft:color-mix(in srgb,var(--accent) 24%,transparent); }
                * { box-sizing:border-box; }
                body { margin:0; min-height:100vh; overflow:auto; color:#f8f8fa; background:{{{wallpaper}}}; }
                body::before { content:''; position:fixed; inset:-120px; pointer-events:none; background:radial-gradient(circle at 18% 12%,var(--accent-soft),transparent 31%),radial-gradient(circle at 84% 78%,#ffbf7a20,transparent 38%); filter:blur(26px); }
                body::after { content:''; position:fixed; inset:0; pointer-events:none; opacity:.16; background-image:linear-gradient(#fff1 1px,transparent 1px),linear-gradient(90deg,#fff1 1px,transparent 1px); background-size:44px 44px; mask-image:linear-gradient(to bottom,black,transparent 78%); }
                main { position:relative; z-index:1; min-height:100vh; width:min(1040px,calc(100vw - 40px)); margin:0 auto; display:flex; flex-direction:column; align-items:center; justify-content:center; padding:54px 0 82px; }
                .private { margin:0 0 18px; padding:9px 15px; border:1px solid #ffffff2d; border-radius:999px; background:#191b20d9; backdrop-filter:blur(24px); color:#d6d7dc; font-size:13px; box-shadow:inset 0 1px 0 #ffffff18; }
                .brand { margin:0; text-align:center; font-size:clamp(42px,6.5vw,72px); font-weight:760; letter-spacing:-4px; line-height:1; text-shadow:0 18px 52px #000b; }
                .brand span { color:var(--accent); }
                .tagline { margin:13px 0 24px; color:#aaaeb8; text-align:center; font-size:14px; }
                .switcher { display:flex; gap:5px; margin:0 0 18px; padding:5px; border:1px solid #ffffff24; border-radius:999px; background:#14161bbd; box-shadow:inset 0 1px 0 #ffffff12; backdrop-filter:blur(24px); }
                .switcher button { min-width:132px; height:40px; padding:0 18px; border:0; border-radius:999px; background:transparent; color:#aeb1ba; font:650 14px inherit; cursor:pointer; transition:.18s ease; }
                .switcher button.active { color:#17181b; background:var(--accent); box-shadow:0 8px 28px var(--accent-soft),inset 0 1px 0 #fff8; }
                .panel { width:min(860px,100%); }
                .hidden { display:none !important; }
                .search-form { height:70px; display:flex; align-items:center; gap:12px; padding:0 12px 0 24px; border:1px solid #ffffff38; border-radius:999px; background:linear-gradient(145deg,#ffffff1e,#ffffff0b); box-shadow:0 28px 82px #0009,inset 0 1px 0 #ffffff3c; backdrop-filter:blur(30px) saturate(1.35); transition:transform .18s ease,border-color .18s ease,box-shadow .18s ease; }
                .search-form:focus-within { transform:translateY(-2px); border-color:color-mix(in srgb,var(--accent) 75%,white); box-shadow:0 34px 92px #000a,0 0 0 4px var(--accent-soft),inset 0 1px 0 #ffffff55; }
                .search-icon { flex:0 0 auto; color:var(--accent); font-size:22px; }
                input,select,textarea { color:#fff; font:500 15px inherit; }
                input::placeholder,textarea::placeholder { color:#858a95; }
                .search-form input { min-width:0; flex:1; border:0; outline:0; background:transparent; font-size:18px; }
                .round-button { width:48px; height:48px; flex:0 0 48px; display:grid; place-items:center; border:0; border-radius:999px; background:var(--accent); color:#17181b; font-size:22px; font-weight:850; cursor:pointer; box-shadow:inset 0 1px 0 #fff8; transition:transform .16s ease,filter .16s ease; }
                .round-button:hover,.action:hover { transform:translateY(-1px); filter:brightness(1.06); }
                .round-button:active,.action:active { transform:scale(.97); }
                .studio { padding:22px; border:1px solid #ffffff2e; border-radius:30px; background:linear-gradient(150deg,#202228e8,#111318e8); box-shadow:0 34px 100px #000a,inset 0 1px 0 #ffffff23; backdrop-filter:blur(32px) saturate(1.25); }
                .studio-head { display:flex; align-items:flex-start; justify-content:space-between; gap:16px; margin-bottom:18px; }
                .studio-title { margin:0; font-size:22px; letter-spacing:-.5px; }
                .studio-note { margin:6px 0 0; color:#979ca7; font-size:13px; line-height:1.45; }
                .badge { flex:0 0 auto; padding:7px 11px; border-radius:999px; border:1px solid color-mix(in srgb,var(--accent) 48%,transparent); background:var(--accent-soft); color:#ffd5b2; font-size:12px; }
                .grid { display:grid; grid-template-columns:1fr 1fr; gap:12px; }
                .field { display:flex; flex-direction:column; gap:7px; }
                .field.wide { grid-column:1/-1; }
                label { color:#c7c9cf; font-size:12px; font-weight:650; }
                .control { width:100%; min-height:45px; border:1px solid #ffffff24; border-radius:15px; outline:0; background:#0f1116c7; padding:11px 13px; transition:.16s ease; }
                .control:focus { border-color:var(--accent); box-shadow:0 0 0 3px var(--accent-soft); }
                select.control { appearance:auto; }
                .options { display:flex; flex-wrap:wrap; gap:9px; margin:14px 0; }
                .check { display:flex; align-items:center; gap:8px; padding:9px 12px; border:1px solid #ffffff20; border-radius:14px; background:#ffffff08; color:#c4c7ce; font-size:13px; }
                .check input { accent-color:var(--accent); }
                .preview { min-height:72px; margin-top:6px; padding:14px 16px; border:1px solid #ffffff24; border-radius:17px; background:#090b0f; color:#ffd7b7; font:500 14px/1.5 'Cascadia Code','Consolas',monospace; overflow-wrap:anywhere; white-space:pre-wrap; }
                .actions { display:flex; flex-wrap:wrap; gap:9px; margin-top:13px; }
                .action { min-height:42px; padding:0 16px; border:1px solid #ffffff25; border-radius:14px; background:#ffffff0d; color:#f2f3f5; font:650 13px inherit; cursor:pointer; transition:.16s ease; }
                .action.primary { border-color:transparent; background:var(--accent); color:#17181b; }
                .subtitle { max-width:680px; margin:18px 24px 0; text-align:center; color:#969ba6; font-size:13px; line-height:1.5; }
                @media(max-width:700px){main{padding:38px 0 58px}.brand{letter-spacing:-2.5px}.grid{grid-template-columns:1fr}.field.wide{grid-column:auto}.studio{padding:17px;border-radius:24px}.studio-head{flex-direction:column}.search-form{height:64px;padding-left:18px}.search-form input{font-size:16px}.switcher button{min-width:118px}.badge{align-self:flex-start}}
              </style>
            </head>
            <body>
              <main>
                {{{privateBadge}}}
                <h1 class="brand">Chroma <span>Browser</span></h1>
                <p class="tagline">{{{WebUtility.HtmlEncode(subtitle)}}}</p>
                <nav class="switcher" aria-label="Режим страницы">
                  <button id="searchTab" class="active" type="button">Поиск</button>
                  <button id="dorkTab" type="button">Dork Studio</button>
                </nav>

                <section id="searchPanel" class="panel">
                  <form class="search-form" action="{{{WebUtility.HtmlEncode(searchAction)}}}" method="get">
                    <span class="search-icon" aria-hidden="true">⌕</span>
                    <input name="{{{WebUtility.HtmlEncode(queryName)}}}" autofocus autocomplete="off" enterkeyhint="search" placeholder="Поиск в интернете">
                    <button class="round-button" type="submit" aria-label="Найти">→</button>
                  </form>
                </section>

                <section id="dorkPanel" class="panel hidden">
                  <div class="studio">
                    <div class="studio-head">
                      <div>
                        <h2 class="studio-title">Конструктор поисковых запросов</h2>
                        <p class="studio-note">Формирует обычные операторы публичного веб-поиска. Используйте только для законного поиска открытой информации.</p>
                      </div>
                      <span class="badge">Public search</span>
                    </div>
                    <div class="grid">
                      <div class="field wide">
                        <label for="target">Что ищем</label>
                        <input id="target" class="control" autocomplete="off" placeholder="Ник, номер, фраза или название">
                      </div>
                      <div class="field">
                        <label for="kind">Тип поиска</label>
                        <select id="kind" class="control">
                          <option value="all">Обычный запрос</option>
                          <option value="nickname">Никнейм</option>
                          <option value="phone">Номер телефона</option>
                          <option value="site">Сайт или домен</option>
                          <option value="title">В заголовке страницы</option>
                          <option value="url">В адресе страницы</option>
                        </select>
                      </div>
                      <div class="field">
                        <label for="filetype">Формат результата</label>
                        <select id="filetype" class="control">
                          <option value="">Все форматы</option>
                          <option value="pdf">PDF</option>
                          <option value="docx">DOCX</option>
                          <option value="xlsx">XLSX</option>
                          <option value="pptx">PPTX</option>
                          <option value="txt">TXT</option>
                        </select>
                      </div>
                      <div class="field">
                        <label for="domain">Искать только на сайте</label>
                        <input id="domain" class="control" autocomplete="off" placeholder="example.com — необязательно">
                      </div>
                      <div class="field">
                        <label for="extra">Дополнительные слова</label>
                        <input id="extra" class="control" autocomplete="off" placeholder="город, тема, организация">
                      </div>
                      <div class="field wide">
                        <label for="exclude">Исключить слова</label>
                        <input id="exclude" class="control" autocomplete="off" placeholder="реклама магазин — необязательно">
                      </div>
                    </div>
                    <div class="options">
                      <label class="check"><input id="exact" type="checkbox" checked> Точное совпадение</label>
                      <label class="check"><input id="documents" type="checkbox"> Только страницы с документами</label>
                    </div>
                    <div id="preview" class="preview">Введите данные — здесь появится готовый запрос.</div>
                    <div class="actions">
                      <button id="copy" class="action" type="button">Копировать</button>
                      <button data-engine="google" class="action primary" type="button">Искать в Google</button>
                      <button data-engine="bing" class="action" type="button">Bing</button>
                      <button data-engine="duck" class="action" type="button">DuckDuckGo</button>
                    </div>
                  </div>
                </section>
                <p class="subtitle">Dork Studio не сканирует сайты и не обходит защиту — он только собирает запрос для выбранной поисковой системы.</p>
              </main>
              <script>
                const searchTab=document.getElementById('searchTab');
                const dorkTab=document.getElementById('dorkTab');
                const searchPanel=document.getElementById('searchPanel');
                const dorkPanel=document.getElementById('dorkPanel');
                const fields=['target','kind','filetype','domain','extra','exclude','exact','documents'].map(id=>document.getElementById(id));
                const preview=document.getElementById('preview');
                let currentQuery='';

                function setMode(mode){
                  const dork=mode==='dork';
                  searchTab.classList.toggle('active',!dork);
                  dorkTab.classList.toggle('active',dork);
                  searchPanel.classList.toggle('hidden',dork);
                  dorkPanel.classList.toggle('hidden',!dork);
                  if(dork) document.getElementById('target').focus();
                }
                searchTab.addEventListener('click',()=>setMode('search'));
                dorkTab.addEventListener('click',()=>setMode('dork'));

                function clean(value){ return value.trim().replace(/\s+/g,' '); }
                function quote(value){ return `"${value.replaceAll('"','').trim()}"`; }
                function domain(value){ return clean(value).replace(/^https?:\/\//i,'').replace(/^www\./i,'').split('/')[0]; }
                function build(){
                  const target=clean(document.getElementById('target').value);
                  const kind=document.getElementById('kind').value;
                  const exact=document.getElementById('exact').checked;
                  const type=document.getElementById('filetype').value;
                  const onlyDomain=domain(document.getElementById('domain').value);
                  const extra=clean(document.getElementById('extra').value);
                  const excluded=clean(document.getElementById('exclude').value).split(' ').filter(Boolean);
                  const docs=document.getElementById('documents').checked;
                  const parts=[];
                  if(target){
                    const value=exact ? quote(target) : target;
                    if(kind==='nickname'){
                      const nick=target.replace(/^@/,'');
                      parts.push(`(${quote(nick)} OR ${quote('@'+nick)})`);
                    }else if(kind==='phone'){
                      const digits=target.replace(/\D/g,'');
                      parts.push(digits.length>=7 && digits!==target ? `(${quote(target)} OR ${quote(digits)})` : value);
                    }else if(kind==='site'){
                      const host=domain(target);
                      if(host) parts.push(`site:${host}`);
                    }else if(kind==='title'){
                      parts.push(`intitle:${value}`);
                    }else if(kind==='url'){
                      parts.push(`inurl:${value}`);
                    }else{
                      parts.push(value);
                    }
                  }
                  if(onlyDomain && kind!=='site') parts.push(`site:${onlyDomain}`);
                  if(extra) parts.push(exact ? quote(extra) : extra);
                  if(type) parts.push(`filetype:${type}`);
                  else if(docs) parts.push('(filetype:pdf OR filetype:docx OR filetype:xlsx OR filetype:pptx)');
                  excluded.forEach(word=>parts.push(`-${word.replace(/^-/,'')}`));
                  currentQuery=parts.join(' ').trim();
                  preview.textContent=currentQuery || 'Введите данные — здесь появится готовый запрос.';
                }
                fields.forEach(field=>{ field.addEventListener('input',build); field.addEventListener('change',build); });
                document.getElementById('copy').addEventListener('click',async()=>{
                  if(!currentQuery) return;
                  try{ await navigator.clipboard.writeText(currentQuery); }
                  catch{ const area=document.createElement('textarea'); area.value=currentQuery; document.body.appendChild(area); area.select(); document.execCommand('copy'); area.remove(); }
                  const button=document.getElementById('copy'); button.textContent='Скопировано'; setTimeout(()=>button.textContent='Копировать',1200);
                });
                document.querySelectorAll('[data-engine]').forEach(button=>button.addEventListener('click',()=>{
                  if(!currentQuery) return;
                  const encoded=encodeURIComponent(currentQuery);
                  const urls={google:`https://www.google.com/search?q=${encoded}`,bing:`https://www.bing.com/search?q=${encoded}`,duck:`https://duckduckgo.com/?q=${encoded}`};
                  window.open(urls[button.dataset.engine], '_blank', 'noopener');
                }));
              </script>
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

    private static string NormalizeAccent(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length == 7 && value[0] == '#' &&
            value.Skip(1).All(Uri.IsHexDigit))
        {
            return value.ToUpperInvariant();
        }

        return "#FF8A3D";
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
                return $"linear-gradient(#08090d75,#08090dcc),url(data:{mediaType};base64,{data}) center/cover fixed";
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Wallpaper could not be loaded: {exception.Message}");
        }

        return "linear-gradient(145deg,#0b0c0f,#202126 52%,#0d0e11)";
    }
}
