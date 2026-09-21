package com.musify.app

// Kotlin/Native has no Gradle-property injection at runtime, so these are
// hardcoded — keep them in sync with mobile/gradle.properties. Point
// `apiBaseUrl`/`authIssuer` at your Mac's reachable address for the backend
// (e.g. the LAN IP of the machine running `dotnet run`, or "localhost" if the
// simulator and the backend are on the same Mac).
actual object AppConfig {
    actual val apiBaseUrl: String = "http://localhost:5111"
    actual val authIssuer: String = "http://localhost:8080"
    actual val authClientId: String = "390116136177434631"

    // Matches CFBundleURLTypes in Info.plist and the redirect URI registered
    // for the "Musify Native" app in Zitadel.
    actual val nativeRedirectUri: String = "musify://callback"
}
