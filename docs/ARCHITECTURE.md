# Chroma Browser architecture

Chroma Browser is a native Windows desktop shell written in C# and WPF. Web content is rendered by Microsoft Edge WebView2, the supported embeddable Chromium runtime on Windows. The application UI is XAML/C# rather than a browser-hosted HTML shell.

## Modules

| Requested area | Implementation |
| --- | --- |
| Core | application lifetime, paths, JSON persistence and rolling logs |
| Browser | tab host, navigation, permissions, process-failure handling and Chromium profiles |
| UI | native WPF title bar, tab strip, address suggestions, Mica backdrop and AI side panel |
| AI | provider registry and isolated top-level WebView2 panel for official provider sites |
| Downloads | WebView2 download operations with pause, resume, cancel, unique paths and persistence |
| Extensions | WebView2 profile extension manager for unpacked Chromium extensions |
| AdBlock | network interception, ABP host rules, cosmetic CSS and updateable subscriptions |
| Updater | GitHub Releases check, background download and mandatory SHA-256 validation before launch |
| Settings | versioned JSON-compatible settings model and native settings window |
| Bookmarks / History | bounded local store, search, delete and Chrome-compatible HTML import/export |
| Passwords | Windows Hello authorization and current-user DPAPI encryption |
| Network / Security | HTTPS-first address resolution, SmartScreen reputation checks, tracking prevention and enhanced security mode |
| Resources | source PNG, multi-resolution ICO and Fluent resource dictionary |

## Profiles and privacy

Normal tabs share `%LOCALAPPDATA%\ChromaBrowser\Chromium`. Incognito windows use a process-specific directory below the system temporary directory. The profile is cleared before disposal and abandoned private directories are removed on the next startup. History and downloads are not persisted for private navigation.

## Extension compatibility

WebView2 supports loading, enabling, disabling and removing unpacked Chromium extensions. It does not expose Chrome Web Store installation as a supported host API, so Chroma does not claim direct store installation or bypass the store's checks. A future full Chromium-source frontend could replace `BrowserTabView` behind the same native service boundaries if direct store compatibility becomes a hard requirement.

## AI data flow

Provider websites run as ordinary top-level pages, so Chroma does not bypass frame restrictions, logins, rate limits or provider terms. When the user requests summarization, translation or explanation, the active tab extracts readable text locally, removes form controls, limits it to 30,000 characters and copies a clearly formed prompt to the clipboard. No Chroma-operated server is involved.

## Update trust

The release workflow creates a self-contained x64 installer and a sibling `.sha256` file. The client will not silently execute an update if the checksum asset is absent. Code-signing can be added to the release workflow when a trusted Windows signing certificate is available; the project does not embed a development certificate or pretend an unsigned binary is signed.

