# Chroma Browser for Android

Native Android 9+ shell for the system Chromium WebView.

## Build

Install JDK 17, Android SDK platform/build-tools 35 and Gradle 8.9, then run:

```bash
gradle --project-dir android testDebugUnitTest lintDebug assembleDebug
```

`app-debug.apk` is installable and uses Android's debug/preview signing identity. Configure a private release keystore outside the repository before Play Store distribution.

## Privacy notes

- normal and incognito browsing run in different processes and WebView data directories;
- private mode does not persist Chroma history, bookmarks or sessions;
- cleartext traffic and invalid TLS certificates are blocked;
- camera, microphone and location access always require an explicit prompt;
- AI page text is copied only after the user presses an action.
