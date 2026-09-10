package com.musify.app

import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.header
import io.ktor.client.request.parameter

/**
 * Thin wrapper over the two endpoints the demo needs:
 *  - `GET /tracks` is public (see TrackEndpoints.cs) — no token required.
 *  - `GET /tracks/{id}/stream` requires a Bearer access token.
 */
class MusifyApi(private val accessTokenProvider: () -> String?) {

    suspend fun getTracks(pageNumber: Int = 1, pageSize: Int = 20, name: String? = null): TracksSearchResponse =
        httpClient.get("${AppConfig.apiBaseUrl}/tracks") {
            parameter("pageNumber", pageNumber)
            parameter("pageSize", pageSize)
            if (!name.isNullOrBlank()) parameter("name", name)
        }.body()

    suspend fun getTrackStream(trackId: String): TrackStreamResponse {
        val token = accessTokenProvider() ?: error("Inicia sesión para reproducir canciones.")
        return httpClient.get("${AppConfig.apiBaseUrl}/tracks/$trackId/stream") {
            header("Authorization", "Bearer $token")
        }.body()
    }

    fun coverUrl(trackId: String): String = "${AppConfig.apiBaseUrl}/tracks/$trackId/cover?size=medium"
}
