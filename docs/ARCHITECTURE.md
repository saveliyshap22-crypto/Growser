# Chroma Browser architecture

Chroma Browser has two native shells around supported platform Chromium runtimes. The Windows shell is C#/.NET 8 and WPF around Microsoft Edge WebView2. The Android shell is Java and Android Views around the system Android WebView. Neither application shell is an HTML application; HTML is used only for ordinary web content such as the new-tab document.

## Platform structure

| Area | Windows implementation | Android implementation |
| --- | --- | --- |
| Core | app lifetime, paths, JSON storage and rolling logs | Activity lifecycle, SharedPreferences JSON and memory callbacks |
| Browser | WebView2 tab views and profiles | detached WebView tab pool and separate private process |
| UI | WPF/XAML, custom title bar, Fluent/Mica resources | Android Views, XML resources and rounded glass drawables |
| AI | resizable right WebView2 panel | animated bottom WebView panel |
| Downloads | WebView2 operations with pause/resume/cancel | operating-system Download Manager |
| Extensions | unpacked WebView2 profile extensions | not exposed by Android System WebView |
| AdBlock | updateable ABP subscriptions and cosmetic rules | host interception plus cosmetic selectors |
| Updater | GitHub Releases, matching checksum and installer launch | release APK distributed through GitHub |
| Settings | versioned native settings model | native settings dialog and private preferences |
| Bookmarks/history | bounded JSON store and HTML import/export | bounded SharedPreferences JSON store |
| Passwords | Windows Hello-gated DPAPI vault | platform WebView/autofill boundary |
| Security | HTTPS-first, SmartScreen, tracking prevention | HTTPS-first, Safe Browsing, TLS cancellation and guarded permissions |

## Windows profiles

Normal tabs share `%LOCALAPPDATA%\ChromaBrowser\Chromium`. Incognito windows use a process-specific directory below the system temporary directory. The profile is cleared before disposal and abandoned private directories are removed on the next startup. History and downloads are not persisted by Chroma for private navigation.

## Android profiles

Normal WebViews live in the default application Chromium profile. `PrivateActivity` runs in the `:incognito` process and configures the `incognito` WebView data-directory suffix before creating a WebView. History, bookmarks and session state are not written from private mode. Cookies, Web Storage, cache, history and form data are cleared when the activity is destroyed.

Background Android tabs are detached from the visible container and paused. The active tab is resumed when selected. Under low-memory callbacks, inactive caches are trimmed without destroying the user's tab list.

## Extension compatibility

WebView2 supports loading, enabling, disabling and removing unpacked Chromium extensions. It does not expose Chrome Web Store installation as a supported host API. Android System WebView does not expose the Chromium extension system. Achieving complete Chrome extension compatibility would require maintaining a full Chromium fork rather than using either supported embedding API.

## AI data flow

Provider websites run as ordinary top-level pages, so Chroma does not bypass frame restrictions, logins, rate limits or provider terms. A page/selection action extracts a bounded amount of readable text locally and copies a clearly formed prompt to the clipboard. The user explicitly pastes or sends it; Chroma does not silently transmit the content.

## Release trust

The release workflow independently builds both platforms and publishes:

- self-contained Windows x64 Setup EXE;
- self-contained Windows x64 portable ZIP;
- installable Android preview APK;
- one sibling `.sha256` file for each package.

The Windows updater chooses the checksum whose filename exactly matches the selected installer asset. Windows Authenticode and stable Android application signing require private keys owned by the publisher; the repository does not embed development secrets or pretend unsigned/preview-signed packages are production-signed.
