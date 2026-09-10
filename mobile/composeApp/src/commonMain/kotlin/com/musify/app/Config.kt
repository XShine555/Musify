package com.musify.app

/**
 * Backend connection settings. Values come from `mobile/gradle.properties`
 * (`musify.*` keys) on Android and Desktop; the iOS actual hardcodes the same
 * values because a Kotlin/Native framework has no Gradle-property injection
 * at runtime. Keep the three in sync when pointing at a different backend.
 *
 * `nativeRedirectUri` is the OAuth redirect registered for the "Musify
 * Native" Zitadel app (see deploy/compose.yml, zitadel-init): a fixed
 * loopback port on Desktop (RFC 8252 native-app loopback flow), and the
 * `musify://callback` custom scheme on Android/iOS.
 */
expect object AppConfig {
    val apiBaseUrl: String
    val authIssuer: String
    val authClientId: String
    val nativeRedirectUri: String
}
