package com.musify.app.auth

import com.musify.app.AppConfig
import com.musify.app.httpClient
import com.musify.app.util.Base64Url
import io.ktor.client.call.body
import io.ktor.client.request.forms.submitForm
import io.ktor.client.request.get
import io.ktor.http.Parameters
import io.ktor.http.decodeURLQueryComponent
import io.ktor.http.encodeURLParameter
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

/** Requesting `offline_access` gets a refresh token back so the session survives an access-token expiry. */
private const val SCOPES = "openid profile email offline_access"

class AuthController(
    private val tokenStore: TokenStore = TokenStore(),
    private val browserLauncher: BrowserAuthLauncher = BrowserAuthLauncher()
) {
    private val _state = MutableStateFlow<AuthState>(AuthState.SignedOut)
    val state: StateFlow<AuthState> = _state.asStateFlow()

    private var discovery: OidcDiscoveryDocument? = null

    /** Call once at startup: restores a still-valid session without touching the browser. */
    suspend fun restoreSession() {
        val stored = tokenStore.load() ?: return
        if (stored.expiresAtEpochSeconds > nowEpochSeconds() + 30) {
            _state.value = AuthState.SignedIn(stored.accessToken, stored.displayName)
        } else {
            tokenStore.clear()
        }
    }

    /** Opens Zitadel's hosted login (with a "Regístrate" link) and completes the PKCE exchange. */
    suspend fun signIn() {
        _state.value = AuthState.SigningIn
        try {
            val doc = discoveryDocument()
            val verifier = Pkce.generateCodeVerifier()
            val challenge = Pkce.codeChallenge(verifier)
            val expectedState = Pkce.randomState()

            val authorizeUrl = buildString {
                append(doc.authorizationEndpoint)
                append("?client_id=").append(AppConfig.authClientId.encodeURLParameter())
                append("&response_type=code")
                append("&redirect_uri=").append(AppConfig.nativeRedirectUri.encodeURLParameter())
                append("&scope=").append(SCOPES.encodeURLParameter())
                append("&code_challenge=").append(challenge)
                append("&code_challenge_method=S256")
                append("&state=").append(expectedState)
            }

            val redirected = browserLauncher.launch(authorizeUrl, AppConfig.nativeRedirectUri)
            val params = parseQuery(redirected.substringAfter('?', ""))

            check(params["state"] == expectedState) { "El estado OAuth no coincide; intenta iniciar sesión de nuevo." }
            params["error"]?.let { error("Zitadel devolvió un error: $it ${params["error_description"].orEmpty()}") }
            val code = params["code"] ?: error("No se recibió el código de autorización.")

            val tokenResponse: TokenResponse = httpClient.submitForm(
                url = doc.tokenEndpoint,
                formParameters = Parameters.build {
                    append("grant_type", "authorization_code")
                    append("code", code)
                    append("redirect_uri", AppConfig.nativeRedirectUri)
                    append("client_id", AppConfig.authClientId)
                    append("code_verifier", verifier)
                }
            ).body()

            val claims = decodeIdTokenClaims(tokenResponse.idToken)
            val displayName = claims?.get("name") ?: claims?.get("preferred_username") ?: claims?.get("email")

            tokenStore.save(
                StoredTokens(
                    accessToken = tokenResponse.accessToken,
                    refreshToken = tokenResponse.refreshToken,
                    expiresAtEpochSeconds = nowEpochSeconds() + tokenResponse.expiresInSeconds,
                    displayName = displayName
                )
            )
            _state.value = AuthState.SignedIn(tokenResponse.accessToken, displayName)
        } catch (t: Throwable) {
            _state.value = AuthState.Error(t.message ?: "No se pudo iniciar sesión.")
        }
    }

    suspend fun signOut() {
        tokenStore.clear()
        _state.value = AuthState.SignedOut
    }

    fun currentAccessToken(): String? = (_state.value as? AuthState.SignedIn)?.accessToken

    private suspend fun discoveryDocument(): OidcDiscoveryDocument {
        discovery?.let { return it }
        val doc: OidcDiscoveryDocument = httpClient
            .get("${AppConfig.authIssuer}/.well-known/openid-configuration")
            .body()
        discovery = doc
        return doc
    }
}

private fun parseQuery(query: String): Map<String, String> =
    query.split("&")
        .filter { it.isNotBlank() }
        .associate { pair ->
            val idx = pair.indexOf('=')
            if (idx < 0) pair to "" else pair.take(idx) to pair.substring(idx + 1).decodeURLQueryComponent()
        }

private fun decodeIdTokenClaims(idToken: String?): Map<String, String>? {
    val payload = idToken?.split(".")?.getOrNull(1) ?: return null
    return try {
        Json.parseToJsonElement(Base64Url.decodeToString(payload)).jsonObject
            .mapValues { it.value.jsonPrimitive.content }
    } catch (t: Throwable) {
        null
    }
}
