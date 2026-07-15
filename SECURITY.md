# Security policy

Please do not publish an exploitable security issue in a public issue.

Use GitHub's private vulnerability reporting for this repository and include:

- affected Chroma Browser version and platform;
- Windows/WebView2 or Android/System WebView versions;
- minimal reproduction steps;
- expected impact;
- logs with tokens, cookies, personal data and local paths removed.

## Security boundaries

- Web content is isolated by the Chromium/WebView2 multi-process sandbox.
- Chroma never disables TLS verification or injects a local root certificate.
- The password vault is encrypted for the current Windows user through DPAPI and gated by Windows Hello.
- Update installers are accepted automatically only when a matching SHA-256 asset is published with the GitHub release.
- Incognito uses a separate temporary Chromium data directory and clears its profile on window close. Operating-system, network-provider, DNS and remote-service logs are outside the browser's control.
- Android incognito runs in a dedicated process with a separate WebView data suffix. Cookies, cache, form data and Web Storage are cleared when it closes; downloads explicitly started by the user remain in Android's Download Manager.
- Android rejects cleartext network traffic by default, cancels invalid TLS certificates and enables the system Safe Browsing implementation.
- AI providers receive page text only after the user presses an AI action. Chroma places the prepared prompt on the clipboard; it does not silently submit page content.
- Release APKs use a CI preview signature until a private Android signing key is configured. They must not be presented as Play Store-signed production packages.

Keep Windows/Android, the platform Chromium runtime and Chroma Browser updated.
