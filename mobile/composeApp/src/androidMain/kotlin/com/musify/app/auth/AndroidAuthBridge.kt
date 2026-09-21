package com.musify.app.auth

import kotlinx.coroutines.CompletableDeferred

/** Hands the redirect URL from [AuthCallbackActivity] back to the coroutine awaiting it in [BrowserAuthLauncher]. */
internal object AndroidAuthBridge {
    @Volatile
    var pending: CompletableDeferred<String>? = null

    fun deliver(redirectUrl: String) {
        pending?.complete(redirectUrl)
        pending = null
    }
}
