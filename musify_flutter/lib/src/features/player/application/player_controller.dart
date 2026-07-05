import 'dart:async';

import 'package:audioplayers/audioplayers.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../tracks/domain/track.dart';
import '../data/stream_repository.dart';

final audioPlayerProvider = Provider<AudioPlayer>((ref) {
  final player = AudioPlayer();
  ref.onDispose(player.dispose);
  return player;
});

class PlaybackState {
  const PlaybackState({
    this.track,
    this.playing = false,
    this.loading = false,
    this.position = Duration.zero,
    this.duration = Duration.zero,
    this.error,
  });

  final Track? track;
  final bool playing;
  final bool loading;
  final Duration position;
  final Duration duration;
  final String? error;

  bool get hasContent => track != null;

  PlaybackState copyWith({
    Track? track,
    bool? playing,
    bool? loading,
    Duration? position,
    Duration? duration,
    Object? error = _sentinel,
  }) {
    return PlaybackState(
      track: track ?? this.track,
      playing: playing ?? this.playing,
      loading: loading ?? this.loading,
      position: position ?? this.position,
      duration: duration ?? this.duration,
      error: identical(error, _sentinel) ? this.error : error as String?,
    );
  }

  static const _sentinel = Object();
}

class PlayerController extends Notifier<PlaybackState> {
  AudioPlayer get _player => ref.read(audioPlayerProvider);

  @override
  PlaybackState build() {
    final player = ref.read(audioPlayerProvider);
    final subscriptions = <StreamSubscription<dynamic>>[
      player.onPlayerStateChanged.listen((value) {
        final playing = value == PlayerState.playing;
        state = state.copyWith(
          playing: playing,
          loading: playing ? false : state.loading,
        );
      }),
      player.onPositionChanged.listen((value) {
        state = state.copyWith(position: value);
      }),
      player.onDurationChanged.listen((value) {
        state = state.copyWith(duration: value, loading: false);
      }),
      player.onPlayerComplete.listen((_) {
        state = state.copyWith(playing: false, position: Duration.zero);
      }),
    ];
    ref.onDispose(() {
      for (final subscription in subscriptions) {
        subscription.cancel();
      }
    });
    return const PlaybackState();
  }

  Future<void> play(Track track) async {
    state = PlaybackState(track: track, loading: true);
    try {
      final stream = await ref.read(streamRepositoryProvider).getStream(track.id);
      await _player.play(UrlSource(_withTicket(stream.manifestUrl, stream.ticket)));
    } catch (error) {
      state = state.copyWith(loading: false, error: _describe(error));
    }
  }

  Future<void> togglePlay() {
    return _player.state == PlayerState.playing
        ? _player.pause()
        : _player.resume();
  }

  Future<void> seek(Duration position) => _player.seek(position);

  Future<void> stop() async {
    await _player.stop();
    state = const PlaybackState();
  }

  String _withTicket(String url, String ticket) {
    final separator = url.contains('?') ? '&' : '?';
    return '$url${separator}t=${Uri.encodeComponent(ticket)}';
  }

  String _describe(Object error) {
    if (error.toString().contains('401')) {
      return 'Inicia sesión para reproducir.';
    }
    return 'No se pudo reproducir la pista.';
  }
}

final playerControllerProvider =
    NotifierProvider<PlayerController, PlaybackState>(PlayerController.new);
