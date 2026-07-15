# Chroma Browser

Chroma Browser is a native Chromium-based browser for Windows and Android. The application shell is native on both platforms: WPF/XAML on Windows and Android Views/Java on Android. Web content is rendered by Microsoft Edge WebView2 on Windows and the system Chromium WebView on Android.

![Chroma Browser icon](src/Chroma.Browser/Resources/Chroma.png)

## Chroma Browser 0.3

Version 0.3 introduces a cleaner home screen and clearer navigation. The start page now focuses on one large central search field without a grid of external links. On Android, open tab capsules and the primary navigation are placed at the bottom for faster switching.

| Capability | Windows | Android |
| --- | --- | --- |
| Chromium runtime | Edge WebView2 | System Android WebView |
| Native rounded UI | WPF, Mica and Fluent glass surfaces | Android Views and rounded glass surfaces |
| Home screen | large central search, no external shortcut grid | large central search, no external shortcut grid |
| Tabs and session restore | drag, pin, group, suspend and restore | bottom tab capsules, overview, duplication and restore |
| Privacy | isolated temporary incognito profile | dedicated incognito process and data suffix |
| Ad blocking | updateable EasyList/EasyPrivacy/uBlock/RU/URLhaus subscriptions | built-in host interception and cosmetic cleanup |
| Bookmarks and history | search, import and export | local lists and private-mode suppression |
| Downloads | pause, resume, cancel and persistence | Android Download Manager |
| AI | resizable right panel | animated bottom panel |
| Password security | Windows Hello and DPAPI vault | Android WebView autofill integration |
| Extensions | unpacked WebView2 Chromium extensions | unavailable in Android system WebView |
| Packaging | Setup EXE and portable self-contained ZIP | directly installable preview APK |

Additional shared behavior includes HTTPS-first address resolution, Safe Browsing/SmartScreen integration, file upload, fullscreen media, camera/microphone/location permission prompts, page translation, find-in-page, desktop-site mode, sharing and crash recovery.

## Windows features

- native .NET 8 WPF shell with rounded controls, tab capsules, panels, tooltips and context menus;
- minimal new-tab page with a prominent search field and optional wallpaper;
- draggable, pinned and grouped tabs, closed-tab restore, mute and background suspension;
- address suggestions, search commands, strict tracking prevention and enhanced Chromium security;
- EasyList, EasyPrivacy, uBlock filters, RU AdList and URLhaus subscriptions;
- bookmark HTML import/export, searchable history and controlled downloads;
- Mistral, Qwen, DeepSeek and OpenRouter official web panels;
- unpacked Chromium extension management;
- Windows Hello + current-user DPAPI password vault;
- Mica, light/dark modes, custom accent and transparency;
- SHA-256-verified GitHub Release updates.

## Android features

- Android 9 or newer, hardware acceleration and current system Chromium WebView;
- minimal new-tab page with one large search field and no fixed external links;
- bottom tab capsules plus clear Home, Tabs, Bookmarks, AI and Menu navigation;
- up to 20 tabs with inactive-tab pausing and low-memory cache trimming;
- separate `:incognito` process with its own WebView data directory and cleanup on exit;
- HTTPS-first navigation, Safe Browsing, invalid-certificate blocking and a compact tracker list;
- bookmarks, history, session restore, search-engine selection and browsing-data cleanup;
- Android Download Manager, file chooser, fullscreen video and guarded site permissions;
- desktop-site mode, page translation, sharing and find-in-page;
- bottom AI panel with user-triggered page/selection extraction. Chroma copies the prompt and never silently submits page data.

The Android preview APK is signed with a temporary CI key so it can be installed immediately. It is not a Play Store package. Stable store distribution and seamless APK-to-APK upgrades require a private publisher-owned signing key that must not be committed to this public repository.

## Platform boundaries

WebView2 supports loading unpacked Chromium extensions but does not provide direct Chrome Web Store installation. Android's system WebView does not expose a browser extension API. Chroma documents these boundaries instead of bypassing store restrictions or claiming unsupported compatibility.

Free AI access, accounts and quotas are controlled by each provider. Chroma opens official provider websites and does not circumvent authentication, limits or terms.

## Build Windows

Requirements: Windows 10 22H2 or Windows 11 x64, .NET 8 SDK and the Edge WebView2 Runtime.

```powershell
dotnet restore ChromaBrowser.sln -p:PublishReadyToRun=true
dotnet build ChromaBrowser.sln -c Release --no-restore
dotnet test ChromaBrowser.sln -c Release --no-build
dotnet run --project src/Chroma.Browser/Chroma.Browser.csproj
```

Portable self-contained folder:

```powershell
dotnet publish src/Chroma.Browser/Chroma.Browser.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishReadyToRun=true -o artifacts/publish
```

## Build Android

Requirements: JDK 17, Android SDK 35 and Gradle 8.9.

```bash
gradle --project-dir android testDebugUnitTest lintDebug assembleDebug
```

The installable preview APK is created at `android/app/build/outputs/apk/debug/app-debug.apk`.

## Release process

Every pull request builds and tests Windows and Android independently. A `v*` tag, a manual workflow run, or an explicit `Release Chroma Browser vX.Y.Z` merge commit runs the release workflow. It repeats tests and lint, creates the Windows installer and portable archive, builds the Android preview APK, generates a SHA-256 file for every package and publishes one GitHub Release.

## Data locations

- Windows profile and settings: `%LOCALAPPDATA%\ChromaBrowser`;
- Windows logs: `%LOCALAPPDATA%\ChromaBrowser\Logs`;
- Windows private profiles: `%TEMP%\ChromaBrowser\Private-*`;
- Android normal data: the app's private storage and Chromium WebView profile;
- Android incognito data: a separate WebView data suffix in the isolated `:incognito` process.

## License

MIT. Filter subscriptions, Android System WebView and the WebView2 Runtime retain their own licenses and terms.
