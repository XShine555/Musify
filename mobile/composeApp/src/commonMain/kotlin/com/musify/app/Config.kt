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
 * `cat.ikerdemo.musify:/callback` reverse-domain custom scheme on Android/iOS
 * (RFC 8252 §7.1: based on a domain we own, so no other app plausibly claims it).
 */
expect object AppConfig {
    val apiBaseUrl: String
    val authIssuer: String
    val authClientId: String
    val nativeRedirectUri: String
}
