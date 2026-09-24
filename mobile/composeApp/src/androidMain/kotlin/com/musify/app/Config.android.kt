package com.musify.app

// BuildConfig.* is generated from the buildConfigField() calls in composeApp/build.gradle.kts,
// which read mobile/gradle.properties.
actual object AppConfig {
    actual val apiBaseUrl: String = BuildConfig.API_BASE_URL
    actual val authIssuer: String = BuildConfig.AUTH_ISSUER
    actual val authClientId: String = BuildConfig.AUTH_CLIENT_ID

    // Matches the AuthCallbackActivity intent-filter in AndroidManifest.xml and
    // the redirect URI registered for the "Musify Native" app in Zitadel.
    actual val nativeRedirectUri: String = "cat.ikerdemo.musify:/callback"
}
