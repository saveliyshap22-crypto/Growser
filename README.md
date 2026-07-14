# Chroma Browser

Chroma Browser is a real native Windows browser shell powered by Chromium WebView2. It combines a Windows 11/Mica interface with tabs, privacy controls, an integrated content blocker, downloads, bookmarks, history, an AI side panel and isolated incognito windows.

![Chroma Browser icon](src/Chroma.Browser/Resources/Chroma.png)

## Included in 0.1

- native WPF interface; the application shell is not HTML;
- Chromium rendering with PDF, WebGL, WebAssembly and modern web platform support supplied by WebView2;
- draggable, pinned and grouped tabs, closed-tab restore, mute and background suspension;
- address suggestions, search commands and HTTPS-first address resolution;
- separate incognito Chromium profile with cleanup on close;
- EasyList, EasyPrivacy, uBlock filters, RU AdList and URLhaus subscriptions;
- per-site and global blocking controls, cosmetic filtering and blocked-request counter;
- bookmarks with Chrome-compatible HTML import/export, searchable history and session restore;
- download manager with pause, resume, cancel, progress data and safe unique filenames;
- Mistral, Qwen, DeepSeek and OpenRouter official web panels with explicit page/selection actions;
- unpacked Chromium extension install, enable/disable and removal;
- password vault protected by Windows Hello and Windows DPAPI;
- strict tracking prevention, WebView2 enhanced security and SmartScreen reputation checking;
- native settings, Mica, light/dark modes, accent color, transparency and custom new-tab wallpaper;
- self-contained x64 packaging, WebView2 bootstrapper, release checks and SHA-256 verification;
- rolling local diagnostics and Windows CI tests.

## Platform boundary

The supported WebView2 extension API loads unpacked Chromium extensions from a local folder. It does not provide direct Chrome Web Store installation, so the application deliberately does not fake that feature or bypass store restrictions. See [architecture](docs/ARCHITECTURE.md) for the exact boundary.

Free access to AI services is controlled by each provider and can change. Chroma opens their official web interfaces and does not circumvent authentication, quotas or terms.

## Build

Requirements:

- Windows 10 22H2 or Windows 11 x64;
- .NET 8 SDK;
- Microsoft Edge WebView2 Runtime (included with current Windows 11 and installed by the packaged setup when needed).

```powershell
dotnet restore ChromaBrowser.sln
dotnet build ChromaBrowser.sln -c Release
dotnet test ChromaBrowser.sln -c Release
dotnet run --project src/Chroma.Browser/Chroma.Browser.csproj
```

To produce the portable self-contained folder:

```powershell
dotnet publish src/Chroma.Browser/Chroma.Browser.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishReadyToRun=true -o artifacts/publish
```

The `Release` workflow builds the Inno Setup installer and matching checksum. Tagging a commit as `v0.1.0` creates a draft GitHub release for manual verification before publication.

## Data locations

- profile and settings: `%LOCALAPPDATA%\ChromaBrowser`;
- logs: `%LOCALAPPDATA%\ChromaBrowser\Logs`;
- private profiles: `%TEMP%\ChromaBrowser\Private-*`, deleted on close or next startup.

## License

MIT. Filter subscriptions and the WebView2 Runtime retain their own licenses and terms.
