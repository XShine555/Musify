package com.musify.app.auth

import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.coroutines.suspendCancellableCoroutine
import platform.AuthenticationServices.ASWebAuthenticationPresentationContextProvidingProtocol
import platform.AuthenticationServices.ASWebAuthenticationSession
import platform.Foundation.NSObject
import platform.Foundation.NSURL
import platform.UIKit.UIApplication
import platform.UIKit.UIWindow
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

@OptIn(ExperimentalForeignApi::class)
private class PresentationContextProvider : NSObject(), ASWebAuthenticationPresentationContextProvidingProtocol {
    override fun presentationAnchorForWebAuthenticationSession(session: ASWebAuthenticationSession): UIWindow {
        @Suppress("UNCHECKED_CAST")
        val windows = UIApplication.sharedApplication.windows as List<UIWindow>
        return windows.firstOrNull() ?: UIWindow()
    }
}

/**
 * `ASWebAuthenticationSession` owns the whole round trip: it opens a system
 * browser sheet at [authorizeUrl] (Zitadel's hosted login, with its
 * "Regístrate" link) and intercepts the `cat.ikerdemo.musify:` redirect itself, no app
 * delegate / `onOpenURL` wiring required.
 */
@OptIn(ExperimentalForeignApi::class)
actual class BrowserAuthLauncher actual constructor() {
    actual suspend fun launch(authorizeUrl: String, redirectUri: String): String =
        suspendCancellableCoroutine { continuation ->
            // `scheme:/path` (RFC 8252 private-use scheme): everything before the first ':'.
            val scheme = redirectUri.substringBefore(":")
            val contextProvider = PresentationContextProvider()

            val session = ASWebAuthenticationSession(
                uRL = NSURL(string = authorizeUrl),
                callbackURLScheme = scheme,
                completionHandler = { callbackUrl, error ->
                    when {
                        callbackUrl != null -> continuation.resume(callbackUrl.absoluteString ?: "")
                        error != null -> continuation.resumeWithException(RuntimeException(error.localizedDescription))
                        else -> continuation.resumeWithException(RuntimeException("Inicio de sesión cancelado."))
                    }
                }
            )
            session.presentationContextProvider = contextProvider
            session.prefersEphemeralWebBrowserSession = false
            session.start()
        }
}
