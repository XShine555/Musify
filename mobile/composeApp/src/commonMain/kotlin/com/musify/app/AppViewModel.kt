package com.musify.app

import com.musify.app.audio.AudioPlayer
import com.musify.app.audio.PlaybackState
import com.musify.app.auth.AuthController
import com.musify.app.auth.AuthState
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class TrackItem(
    val id: String,
    val title: String,
    val artist: String?,
    val coverUrl: String
)

sealed interface LoadState {
    data object Loading : LoadState
    data object Idle : LoadState
    data class Error(val message: String) : LoadState
}

/**
 * Plain coroutine-scoped holder (not an androidx ViewModel, to keep the demo
 * free of a platform-specific DI/factory dance) wiring auth, the catalog and
 * playback together for [App].
 */
class AppViewModel {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)

    val auth = AuthController()
    private val api = MusifyApi(accessTokenProvider = { auth.currentAccessToken() })
    val player = AudioPlayer()

    val authState: StateFlow<AuthState> = auth.state
    val playbackState: StateFlow<PlaybackState> = player.state

    private val _tracks = MutableStateFlow<List<TrackItem>>(emptyList())
    val tracks: StateFlow<List<TrackItem>> = _tracks.asStateFlow()

    private val _loadState = MutableStateFlow<LoadState>(LoadState.Idle)
    val loadState: StateFlow<LoadState> = _loadState.asStateFlow()

    private val _nowPlaying = MutableStateFlow<TrackItem?>(null)
    val nowPlaying: StateFlow<TrackItem?> = _nowPlaying.asStateFlow()

    private val _playbackError = MutableStateFlow<String?>(null)
    val playbackError: StateFlow<String?> = _playbackError.asStateFlow()

    init {
        scope.launch { auth.restoreSession() }
        refreshTracks()
    }

    fun signIn() {
        scope.launch { auth.signIn() }
    }

    fun signOut() {
        scope.launch {
            player.stop()
            _nowPlaying.value = null
            auth.signOut()
        }
    }

    fun refreshTracks() {
        scope.launch {
            _loadState.value = LoadState.Loading
            try {
                val response = api.getTracks(pageSize = 30)
                _tracks.value = response.items.mapNotNull { item ->
                    item.track?.let { track ->
                        TrackItem(
                            id = track.id,
                            title = track.title,
                            artist = track.artist,
                            coverUrl = api.coverUrl(track.id)
                        )
                    }
                }
                _loadState.value = LoadState.Idle
            } catch (t: Throwable) {
                _loadState.value = LoadState.Error(t.message ?: "No se pudo cargar el catálogo.")
            }
        }
    }

    fun playTrack(track: TrackItem) {
        scope.launch {
            _playbackError.value = null
            try {
                val stream = api.getTrackStream(track.id)
                _nowPlaying.value = track
                player.play(stream.playableUrl())
            } catch (t: Throwable) {
                _playbackError.value = t.message ?: "No se pudo reproducir la canción."
            }
        }
    }

    fun togglePlayPause() {
        when (playbackState.value) {
            PlaybackState.PLAYING -> player.pause()
            PlaybackState.PAUSED -> player.resume()
            else -> Unit
        }
    }

    fun dispose() {
        player.dispose()
        scope.cancel()
    }
}
