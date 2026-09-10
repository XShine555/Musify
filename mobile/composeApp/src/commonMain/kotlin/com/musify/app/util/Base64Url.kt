package com.musify.app.util

private const val ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"

/**
 * Minimal, dependency-free base64url codec (RFC 4648 §5, no padding) used for
 * the PKCE code challenge and for peeking at the ID token's payload. Kept
 * hand-rolled instead of `kotlin.io.encoding.Base64` so the demo doesn't
 * depend on a still-evolving experimental stdlib API.
 */
object Base64Url {
    fun encode(bytes: ByteArray): String {
        val sb = StringBuilder()
        var i = 0
        while (i < bytes.size) {
            val b0 = bytes[i].toInt() and 0xFF
            val b1 = if (i + 1 < bytes.size) bytes[i + 1].toInt() and 0xFF else 0
            val b2 = if (i + 2 < bytes.size) bytes[i + 2].toInt() and 0xFF else 0
            sb.append(ALPHABET[b0 shr 2])
            sb.append(ALPHABET[((b0 and 0x3) shl 4) or (b1 shr 4)])
            if (i + 1 < bytes.size) sb.append(ALPHABET[((b1 and 0xF) shl 2) or (b2 shr 6)])
            if (i + 2 < bytes.size) sb.append(ALPHABET[b2 and 0x3F])
            i += 3
        }
        return sb.toString().replace('+', '-').replace('/', '_')
    }

    fun decode(value: String): ByteArray {
        val cleaned = value.replace('-', '+').replace('_', '/')
        val padded = cleaned + "=".repeat((4 - cleaned.length % 4) % 4)
        val output = ArrayList<Byte>(padded.length / 4 * 3)
        var i = 0
        while (i < padded.length) {
            val c0 = ALPHABET.indexOf(padded[i])
            val c1 = ALPHABET.indexOf(padded[i + 1])
            val c2 = if (padded[i + 2] != '=') ALPHABET.indexOf(padded[i + 2]) else -1
            val c3 = if (padded[i + 3] != '=') ALPHABET.indexOf(padded[i + 3]) else -1

            output.add(((c0 shl 2) or (c1 shr 4)).toByte())
            if (c2 >= 0) output.add((((c1 and 0xF) shl 4) or (c2 shr 2)).toByte())
            if (c3 >= 0) output.add((((c2 and 0x3) shl 6) or c3).toByte())
            i += 4
        }
        return output.toByteArray()
    }

    fun decodeToString(value: String): String = decode(value).decodeToString()
}
