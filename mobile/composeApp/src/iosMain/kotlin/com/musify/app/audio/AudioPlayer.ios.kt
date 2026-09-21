package com.musify.app.audio

import kotlinx.cinterop.ExperimentalForeignApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import platform.AVFoundation.AVPlayer
import platform.AVFoundation.AVPlayerItem
import platform.AVFoundation.pause
import platform.AVFoundation.play
import platform.AVFoundation.replaceCurrentItemWithPlayerItem
import platform.Foundation.NSURL

/**
 * A deliberately simple `AVPlayer` wrapper: it reports PLAYING/PAUSED
 * optimistically on `play()`/`pause()` rather than observing the item's
 * `status`/`timeControlStatus` via KVO, to keep this file's Objective-C
 * interop small enough to review without a Mac to compile-check it against.
 */
@OptIn(ExperimentalForeignApi::class)
actual class AudioPlayer actual constructor() {
    private val _state = MutableStateFlow(PlaybackState.IDLE)
    actual val state: StateFlow<PlaybackState> = _state.asStateFlow()

    private val player = AVPlayer()

    actual fun play(url: String) {
        _state.value = PlaybackState.LOADING
        val item = AVPlayerItem(uRL = NSURL(string = url)!!)
        player.replaceCurrentItemWithPlayerItem(item)
        player.play()
        _state.value = PlaybackState.PLAYING
    }

    actual fun pause() {
        player.pause()
        _state.value = PlaybackState.PAUSED
    }

    actual fun resume() {
        player.play()
        _state.value = PlaybackState.PLAYING
    }

    actual fun stop() {
        player.pause()
        player.replaceCurrentItemWithPlayerItem(null)
        _state.value = PlaybackState.IDLE
    }

    actual fun dispose() {
        stop()
    }
}
