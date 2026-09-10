package com.musify.app.auth

/**
 * Opens the system/in-app browser at [authorizeUrl] and suspends until Zitadel
 * redirects back to [redirectUri], returning the full redirect URL (including
 * its `?code=...&state=...` or `?error=...` query string).
 *
 * Zitadel's hosted login page (opened here) is also where "¿No tienes cuenta?
 * Regístrate" lives — the demo doesn't need a separate register screen or
 * endpoint, the same Authorization Code + PKCE round trip covers both.
 *
 *  - Desktop: a one-shot loopback HTTP server on the fixed port registered in
 *    Zitadel (RFC 8252 §7.3), opened via the OS default browser.
 *  - Android: Chrome Custom Tabs; the redirect is caught by
 *    [AuthCallbackActivity] via the `musify://callback` intent filter.
 *  - iOS: `ASWebAuthenticationSession`, which owns the whole round trip.
 */
expect class BrowserAuthLauncher() {
    suspend fun launch(authorizeUrl: String, redirectUri: String): String
}
