package com.musify.app

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonElement

/** Mirrors `Musify.Application.Tracks.Responses.TrackApplicationResponse`. */
@Serializable
data class TrackApplicationResponse(
    val id: String,
    val title: String,
    val artist: String? = null,
    val source: String,
    val externalId: String? = null,
    val audioStatus: String,
    val duration: Double,
    val listensCount: Int,
    val ownerUserId: Long? = null,
    val createdAt: String,
    val updatedAt: String
)

/**
 * Mirrors `TrackSearchItemResponse`: exactly one of [track] / [youTubeSong] is
 * populated. The demo only lists local catalog tracks, so [youTubeSong] is
 * kept untyped (raw JSON) rather than modeled in full.
 */
@Serializable
data class TrackSearchItemResponse(
    val source: String,
    val track: TrackApplicationResponse? = null,
    val youTubeSong: JsonElement? = null
)

/** Mirrors `TracksSearchResponse`. */
@Serializable
data class TracksSearchResponse(
    val items: List<TrackSearchItemResponse>,
    val pageNumber: Int,
    val pageSize: Int,
    val pageCount: Int,
    val totalItemCount: Int,
    val hasPreviousPage: Boolean,
    val hasNextLocalPage: Boolean,
    val nextYoutubeContinuationToken: String? = null,
    val youtubeUnavailable: Boolean = false,
    val hasNextPage: Boolean = false
)

/** Mirrors `TrackStreamResponse`: a manifest URL plus a short-lived signed ticket. */
@Serializable
data class TrackStreamResponse(
    val manifestUrl: String,
    val ticket: String,
    val expiresInSeconds: Int
)

/** The final URL the streaming gateway accepts: `ManifestUrl?t=Ticket` (see TicketValidationMiddleware). */
fun TrackStreamResponse.playableUrl(): String {
    val separator = if (manifestUrl.contains("?")) "&" else "?"
    return "$manifestUrl${separator}t=$ticket"
}
