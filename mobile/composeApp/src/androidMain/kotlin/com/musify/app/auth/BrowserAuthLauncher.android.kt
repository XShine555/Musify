package com.musify.app.auth

import android.content.Intent
import android.net.Uri
import androidx.browser.customtabs.CustomTabsIntent
import com.musify.app.AppContextHolder
import kotlinx.coroutines.CompletableDeferred

actual class BrowserAuthLauncher actual constructor() {
    actual suspend fun launch(authorizeUrl: String, redirectUri: String): String {
        val deferred = CompletableDeferred<String>()
        AndroidAuthBridge.pending = deferred

        val customTabsIntent = CustomTabsIntent.Builder().build()
        customTabsIntent.intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        customTabsIntent.launchUrl(AppContextHolder.appContext, Uri.parse(authorizeUrl))

        return deferred.await()
    }
}
