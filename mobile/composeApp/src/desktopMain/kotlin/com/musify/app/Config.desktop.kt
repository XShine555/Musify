package com.musify.app

// Passed as -Dmusify.* JVM args by the `compose.desktop.application` block
// (composeApp/build.gradle.kts), sourced from mobile/gradle.properties.
actual object AppConfig {
    actual val apiBaseUrl: String = System.getProperty("musify.apiBaseUrl", "http://localhost:5111")
    actual val authIssuer: String = System.getProperty("musify.authIssuer", "http://host.docker.internal:8080")
    actual val authClientId: String = System.getProperty("musify.authClientId", "")
    actual val nativeRedirectUri: String = "http://127.0.0.1:17453/callback"
}
