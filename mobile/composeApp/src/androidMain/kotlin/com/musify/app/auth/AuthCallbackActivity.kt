package com.musify.app.auth

import android.app.Activity
import android.content.Intent
import android.os.Bundle

/**
 * Declared in AndroidManifest.xml with an intent-filter for `musify://callback`.
 * Chrome Custom Tabs redirects here once Zitadel finishes the login/register
 * flow; this activity just forwards the URL to [AndroidAuthBridge] and closes.
 */
class AuthCallbackActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        deliverAndFinish(intent)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        deliverAndFinish(intent)
    }

    private fun deliverAndFinish(intent: Intent?) {
        intent?.data?.toString()?.let { AndroidAuthBridge.deliver(it) }
        finish()
    }
}
