package com.musify.app.auth

import com.sun.net.httpserver.HttpServer
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.awt.Desktop
import java.net.InetSocketAddress
import java.net.URI

/**
 * RFC 8252 §7.3 "loopback interface redirection": a one-shot local HTTP
 * server receives the Authorization Code callback in the system browser, then
 * shuts itself down. `redirectUri` must match `http://127.0.0.1:17453/callback`
 * exactly, as registered for the "Musify Native" Zitadel app.
 */
actual class BrowserAuthLauncher actual constructor() {
    actual suspend fun launch(authorizeUrl: String, redirectUri: String): String {
        val redirect = URI(redirectUri)
        val port = redirect.port.takeIf { it > 0 } ?: error("El redirect URI de Desktop necesita un puerto explícito.")
        val path = redirect.path.ifEmpty { "/" }

        val result = CompletableDeferred<String>()
        val server = HttpServer.create(InetSocketAddress("127.0.0.1", port), 0)
        server.createContext(path) { exchange ->
            val query = exchange.requestURI.rawQuery.orEmpty()
            val body = """
                <html><body style="font-family:sans-serif;padding:2rem">
                <h2>Musify</h2><p>Ya puedes volver a la app y cerrar esta pestaña.</p>
                </body></html>
            """.trimIndent().toByteArray()
            exchange.responseHeaders.add("Content-Type", "text/html; charset=utf-8")
            exchange.sendResponseHeaders(200, body.size.toLong())
            exchange.responseBody.use { it.write(body) }
            result.complete("$redirectUri?$query")
        }
        server.start()

        return try {
            withContext(Dispatchers.IO) { Desktop.getDesktop().browse(URI(authorizeUrl)) }
            result.await()
        } finally {
            server.stop(0)
        }
    }
}
