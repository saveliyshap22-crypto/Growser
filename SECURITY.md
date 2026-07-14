# Security policy

Please do not publish an exploitable security issue in a public issue.

Use GitHub's private vulnerability reporting for this repository and include:

- affected Chroma Browser version;
- Windows and WebView2 Runtime versions;
- minimal reproduction steps;
- expected impact;
- logs with tokens, cookies, personal data and local paths removed.

## Security boundaries

- Web content is isolated by the Chromium/WebView2 multi-process sandbox.
- Chroma never disables TLS verification or injects a local root certificate.
- The password vault is encrypted for the current Windows user through DPAPI and gated by Windows Hello.
- Update installers are accepted automatically only when a matching SHA-256 asset is published with the GitHub release.
- Incognito uses a separate temporary Chromium data directory and clears its profile on window close. Operating-system, network-provider, DNS and remote-service logs are outside the browser's control.
- AI providers receive page text only after the user presses an AI action. Chroma places the prepared prompt on the clipboard; it does not silently submit page content.

Keep Windows, the Microsoft Edge WebView2 Runtime and Chroma Browser updated.

