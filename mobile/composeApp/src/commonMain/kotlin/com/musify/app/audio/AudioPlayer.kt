package com.musify.app.audio

import kotlinx.coroutines.flow.StateFlow

enum class PlaybackState { IDLE, LOADING, PLAYING, PAUSED, ERROR }

/**
 * Streams a single URL — the gateway-signed `manifestUrl?t=ticket` from
 * [com.musify.app.TrackStreamResponse.playableUrl] — with the platform's
 * native audio stack: Media3/ExoPlayer on Android, JavaFX `MediaPlayer` on
 * Desktop, `AVPlayer` on iOS. No custom headers are needed: the ticket
 * travels as a query parameter, so any plain HTTP media player works.
 */
expect class AudioPlayer() {
    val state: StateFlow<PlaybackState>
    fun play(url: String)
    fun pause()
    fun resume()
    fun stop()
    fun dispose()
}
