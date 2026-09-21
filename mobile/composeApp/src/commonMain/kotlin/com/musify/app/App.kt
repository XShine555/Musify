package com.musify.app

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.musify.app.audio.PlaybackState
import com.musify.app.auth.AuthState

private val MusifyGreen = Color(0xFF1DB954)

@Composable
fun App() {
    val viewModel = remember { AppViewModel() }
    DisposableEffect(Unit) {
        onDispose { viewModel.dispose() }
    }

    MaterialTheme(colorScheme = darkColorScheme(primary = MusifyGreen)) {
        HomeScreen(viewModel)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun HomeScreen(viewModel: AppViewModel) {
    val authState by viewModel.authState.collectAsState()
    val tracks by viewModel.tracks.collectAsState()
    val loadState by viewModel.loadState.collectAsState()
    val nowPlaying by viewModel.nowPlaying.collectAsState()
    val playbackState by viewModel.playbackState.collectAsState()
    val playbackError by viewModel.playbackError.collectAsState()

    Scaffold(
        topBar = { MusifyTopBar(authState, onSignIn = viewModel::signIn, onSignOut = viewModel::signOut) },
        bottomBar = {
            nowPlaying?.let {
                MiniPlayerBar(
                    track = it,
                    playbackState = playbackState,
                    onToggle = viewModel::togglePlayPause
                )
            }
        }
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            playbackError?.let { message ->
                Surface(color = MaterialTheme.colorScheme.errorContainer, modifier = Modifier.fillMaxWidth()) {
                    Text(message, Modifier.padding(12.dp), color = MaterialTheme.colorScheme.onErrorContainer)
                }
            }

            when (val state = loadState) {
                is LoadState.Loading -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }

                is LoadState.Error -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text(state.message, color = MaterialTheme.colorScheme.error)
                        Spacer(Modifier.height(8.dp))
                        Button(onClick = viewModel::refreshTracks) { Text("Reintentar") }
                    }
                }

                LoadState.Idle -> if (tracks.isEmpty()) {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Text("Todavía no hay canciones en el catálogo.")
                    }
                } else {
                    TrackList(tracks, nowPlaying?.id, onPlay = viewModel::playTrack)
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MusifyTopBar(authState: AuthState, onSignIn: () -> Unit, onSignOut: () -> Unit) {
    TopAppBar(
        title = { Text("Musify", fontWeight = FontWeight.Bold) },
        actions = {
            when (authState) {
                is AuthState.SignedIn ->
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text(authState.displayName ?: "Cuenta", Modifier.padding(end = 8.dp))
                        TextButton(onClick = onSignOut) { Text("Cerrar sesión") }
                    }

                AuthState.SigningIn ->
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        CircularProgressIndicator(Modifier.size(20.dp))
                        Spacer(Modifier.width(8.dp))
                        Text("Abriendo navegador…")
                    }

                is AuthState.Error, AuthState.SignedOut ->
                    OutlinedButton(onClick = onSignIn, modifier = Modifier.padding(end = 12.dp)) {
                        Text("Iniciar sesión / Registrarse")
                    }
            }
        }
    )
}

@Composable
private fun TrackList(tracks: List<TrackItem>, playingId: String?, onPlay: (TrackItem) -> Unit) {
    LazyColumn(contentPadding = PaddingValues(vertical = 8.dp)) {
        items(tracks, key = { it.id }) { track ->
            TrackRow(track, isPlaying = track.id == playingId, onClick = { onPlay(track) })
        }
    }
}

@Composable
private fun TrackRow(track: TrackItem, isPlaying: Boolean, onClick: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .background(if (isPlaying) MusifyGreen.copy(alpha = 0.15f) else Color.Transparent)
            .padding(horizontal = 16.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        AsyncImage(
            model = track.coverUrl,
            contentDescription = track.title,
            modifier = Modifier.size(48.dp).clip(RoundedCornerShape(6.dp))
        )
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Text(track.title, maxLines = 1, overflow = TextOverflow.Ellipsis, fontWeight = FontWeight.Medium)
            track.artist?.let { Text(it, maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.bodySmall) }
        }
        Text(
            text = if (isPlaying) "⏸" else "▶",
            color = if (isPlaying) MusifyGreen else MaterialTheme.colorScheme.onSurface
        )
    }
}

@Composable
private fun MiniPlayerBar(track: TrackItem, playbackState: PlaybackState, onToggle: () -> Unit) {
    Surface(shadowElevation = 8.dp) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            AsyncImage(
                model = track.coverUrl,
                contentDescription = track.title,
                modifier = Modifier.size(40.dp).clip(RoundedCornerShape(6.dp))
            )
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Text(track.title, maxLines = 1, overflow = TextOverflow.Ellipsis, fontWeight = FontWeight.Medium)
                Text(
                    when (playbackState) {
                        PlaybackState.LOADING -> "Cargando…"
                        PlaybackState.PLAYING -> "Reproduciendo"
                        PlaybackState.PAUSED -> "Pausado"
                        PlaybackState.ERROR -> "Error de reproducción"
                        PlaybackState.IDLE -> ""
                    },
                    style = MaterialTheme.typography.bodySmall
                )
            }
            IconButton(onClick = onToggle) {
                Text(if (playbackState == PlaybackState.PLAYING) "⏸" else "▶")
            }
        }
    }
}
