package com.musify.app.auth

import com.musify.app.jsonConfig
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import platform.Foundation.NSUserDefaults

private const val TOKENS_KEY = "musify_tokens_json"

actual class TokenStore actual constructor() {
    private val defaults = NSUserDefaults.standardUserDefaults

    actual suspend fun save(tokens: StoredTokens) {
        defaults.setObject(jsonConfig.encodeToString(tokens), forKey = TOKENS_KEY)
    }

    actual suspend fun load(): StoredTokens? {
        val json = defaults.stringForKey(TOKENS_KEY) ?: return null
        return try {
            jsonConfig.decodeFromString<StoredTokens>(json)
        } catch (t: Throwable) {
            null
        }
    }

    actual suspend fun clear() {
        defaults.removeObjectForKey(TOKENS_KEY)
    }
}
