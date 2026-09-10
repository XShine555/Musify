package com.musify.app.auth

import com.musify.app.jsonConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import java.io.File

actual class TokenStore actual constructor() {
    private val file = File(System.getProperty("user.home"), ".musify/tokens.json")

    actual suspend fun save(tokens: StoredTokens) = withContext(Dispatchers.IO) {
        file.parentFile?.mkdirs()
        file.writeText(jsonConfig.encodeToString(tokens))
    }

    actual suspend fun load(): StoredTokens? = withContext(Dispatchers.IO) {
        if (!file.exists()) return@withContext null
        try {
            jsonConfig.decodeFromString<StoredTokens>(file.readText())
        } catch (t: Throwable) {
            null
        }
    }

    actual suspend fun clear(): Unit = withContext(Dispatchers.IO) {
        file.delete()
        Unit
    }
}
