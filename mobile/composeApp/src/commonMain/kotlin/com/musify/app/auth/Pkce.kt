package com.musify.app.auth

import com.musify.app.util.Base64Url
import kotlin.random.Random

/** SHA-256, implemented per platform (java.security on JVM, CommonCrypto on iOS). */
expect fun sha256(input: ByteArray): ByteArray

/** Wall-clock seconds since epoch, needed to persist token expiry across restarts. */
expect fun nowEpochSeconds(): Long

/** RFC 7636 PKCE helpers for the Authorization Code flow the native clients use. */
object Pkce {
    private val unreservedChars = (('A'..'Z') + ('a'..'z') + ('0'..'9') + listOf('-', '_', '.', '~'))

    fun generateCodeVerifier(): String {
        val random = Random.Default
        return (1..64).map { unreservedChars[random.nextInt(unreservedChars.size)] }.joinToString("")
    }

    fun codeChallenge(verifier: String): String =
        Base64Url.encode(sha256(verifier.encodeToByteArray()))

    fun randomState(): String = generateCodeVerifier().take(24)
}
