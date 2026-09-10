package com.musify.app.auth

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

/** `GET {issuer}/.well-known/openid-configuration` (OIDC discovery). */
@Serializable
data class OidcDiscoveryDocument(
    val issuer: String,
    @SerialName("authorization_endpoint") val authorizationEndpoint: String,
    @SerialName("token_endpoint") val tokenEndpoint: String,
    @SerialName("end_session_endpoint") val endSessionEndpoint: String? = null
)

/** `POST {token_endpoint}` response (RFC 6749 §5.1). */
@Serializable
data class TokenResponse(
    @SerialName("access_token") val accessToken: String,
    @SerialName("refresh_token") val refreshToken: String? = null,
    @SerialName("id_token") val idToken: String? = null,
    @SerialName("expires_in") val expiresInSeconds: Long = 0,
    @SerialName("token_type") val tokenType: String = "Bearer"
)

/** What [TokenStore] persists locally between app launches. */
@Serializable
data class StoredTokens(
    val accessToken: String,
    val refreshToken: String?,
    val expiresAtEpochSeconds: Long,
    val displayName: String?
)

/** Where the demo's UI is at, driven by [AuthController.state]. */
sealed interface AuthState {
    data object SignedOut : AuthState
    data object SigningIn : AuthState
    data class SignedIn(val accessToken: String, val displayName: String?) : AuthState
    data class Error(val message: String) : AuthState
}
