package com.musify.app.auth

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.musify.app.AppContextHolder
import com.musify.app.jsonConfig
import kotlinx.coroutines.flow.first
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString

private val Context.authDataStore by preferencesDataStore(name = "musify_auth")
private val TOKENS_KEY = stringPreferencesKey("tokens_json")

actual class TokenStore actual constructor() {
    private val context get() = AppContextHolder.appContext

    actual suspend fun save(tokens: StoredTokens) {
        context.authDataStore.edit { prefs -> prefs[TOKENS_KEY] = jsonConfig.encodeToString(tokens) }
    }

    actual suspend fun load(): StoredTokens? {
        val json = context.authDataStore.data.first()[TOKENS_KEY] ?: return null
        return try {
            jsonConfig.decodeFromString<StoredTokens>(json)
        } catch (t: Throwable) {
            null
        }
    }

    actual suspend fun clear() {
        context.authDataStore.edit { it.clear() }
    }
}
