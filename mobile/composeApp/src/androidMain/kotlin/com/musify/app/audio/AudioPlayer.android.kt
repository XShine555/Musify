package com.musify.app.audio

import androidx.media3.common.MediaItem
import androidx.media3.common.PlaybackException
import androidx.media3.common.Player
import androidx.media3.exoplayer.ExoPlayer
import com.musify.app.AppContextHolder
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

actual class AudioPlayer actual constructor() {
    private val _state = MutableStateFlow(PlaybackState.IDLE)
    actual val state: StateFlow<PlaybackState> = _state.asStateFlow()

    private val exoPlayer: ExoPlayer by lazy {
        ExoPlayer.Builder(AppContextHolder.appContext).build().apply {
            addListener(object : Player.Listener {
                override fun onPlaybackStateChanged(playbackState: Int) {
                    _state.value = when (playbackState) {
                        Player.STATE_BUFFERING -> PlaybackState.LOADING
                        Player.STATE_READY -> if (playWhenReady) PlaybackState.PLAYING else PlaybackState.PAUSED
                        Player.STATE_ENDED -> PlaybackState.PAUSED
                        else -> PlaybackState.IDLE
                    }
                }

                override fun onPlayerError(error: PlaybackException) {
                    _state.value = PlaybackState.ERROR
                }
            })
        }
    }

    actual fun play(url: String) {
        _state.value = PlaybackState.LOADING
        exoPlayer.setMediaItem(MediaItem.fromUri(url))
        exoPlayer.prepare()
        exoPlayer.playWhenReady = true
    }

    actual fun pause() {
        exoPlayer.pause()
    }

    actual fun resume() {
        exoPlayer.play()
    }

    actual fun stop() {
        exoPlayer.stop()
        _state.value = PlaybackState.IDLE
    }

    actual fun dispose() {
        exoPlayer.release()
    }
}
