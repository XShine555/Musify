package com.musify.app.auth

/** Local persistence for [StoredTokens] — a file on Desktop, DataStore on Android, NSUserDefaults on iOS. */
expect class TokenStore() {
    suspend fun save(tokens: StoredTokens)
    suspend fun load(): StoredTokens?
    suspend fun clear()
}
