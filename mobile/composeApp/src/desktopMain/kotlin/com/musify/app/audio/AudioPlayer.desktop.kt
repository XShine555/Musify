package com.musify.app.audio

import javafx.application.Platform as JfxPlatform
import javafx.scene.media.Media
import javafx.scene.media.MediaPlayer
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

/**
 * Boots the JavaFX toolkit headlessly (no Stage/window) so `MediaPlayer` can
 * decode/play audio inside a plain Compose Desktop (Swing/Skiko) app.
 */
private object JavaFxBootstrap {
    private var started = false

    @Synchronized
    fun ensureStarted() {
        if (started) return
        started = true
        try {
            JfxPlatform.startup {}
            JfxPlatform.setImplicitExit(false)
        } catch (_: IllegalStateException) {
            // Toolkit already running (e.g. re-entrant init) — nothing to do.
        }
    }
}

actual class AudioPlayer actual constructor() {
    private val _state = MutableStateFlow(PlaybackState.IDLE)
    actual val state: StateFlow<PlaybackState> = _state.asStateFlow()

    private var mediaPlayer: MediaPlayer? = null

    actual fun play(url: String) {
        JavaFxBootstrap.ensureStarted()
        _state.value = PlaybackState.LOADING
        JfxPlatform.runLater {
            mediaPlayer?.stop()
            mediaPlayer?.dispose()
            val player = MediaPlayer(Media(url))
            mediaPlayer = player
            player.setOnPlaying { _state.value = PlaybackState.PLAYING }
            player.setOnPaused { _state.value = PlaybackState.PAUSED }
            player.setOnStopped { _state.value = PlaybackState.IDLE }
            player.setOnEndOfMedia { _state.value = PlaybackState.PAUSED }
            player.setOnError { _state.value = PlaybackState.ERROR }
            player.play()
        }
    }

    actual fun pause() {
        if (mediaPlayer == null) return
        JfxPlatform.runLater { mediaPlayer?.pause() }
    }

    actual fun resume() {
        if (mediaPlayer == null) return
        JfxPlatform.runLater { mediaPlayer?.play() }
    }

    actual fun stop() {
        // Nothing was ever played (JavaFX toolkit never booted) — runLater would
        // throw "Toolkit not initialized" here, e.g. when closing the app cold.
        if (mediaPlayer == null) {
            _state.value = PlaybackState.IDLE
            return
        }
        JfxPlatform.runLater { mediaPlayer?.stop() }
        _state.value = PlaybackState.IDLE
    }

    actual fun dispose() {
        if (mediaPlayer == null) return
        JfxPlatform.runLater {
            mediaPlayer?.dispose()
            mediaPlayer = null
        }
    }
}
