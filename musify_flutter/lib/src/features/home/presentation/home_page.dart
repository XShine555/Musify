import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/cover_art.dart';
import '../../player/application/player_controller.dart';
import '../../playlists/data/playlist_repository.dart';
import '../../playlists/domain/playlist.dart';
import '../../tracks/data/track_repository.dart';
import '../../tracks/domain/track.dart';

class HomePage extends ConsumerWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final playLists = ref.watch(playListsProvider);
    final tracks = ref.watch(tracksProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(playListsProvider);
        ref.invalidate(tracksProvider);
        await Future.wait([
          ref.read(playListsProvider.future),
          ref.read(tracksProvider.future),
        ]);
      },
      child: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          const _SectionHeader(title: 'Tus playlists'),
          SizedBox(
            height: 190,
            child: AsyncValueView<List<PlayList>>(
              value: playLists,
              onRetry: () => ref.invalidate(playListsProvider),
              data: (items) => _PlayListRow(playLists: items),
            ),
          ),
          const _SectionHeader(title: 'Pistas recientes'),
          AsyncValueView<List<Track>>(
            value: tracks,
            onRetry: () => ref.invalidate(tracksProvider),
            data: (items) => _TrackList(
              tracks: items,
              onPlay: (track) =>
                  ref.read(playerControllerProvider.notifier).play(track),
            ),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Text(title, style: Theme.of(context).textTheme.titleLarge),
    );
  }
}

class _PlayListRow extends StatelessWidget {
  const _PlayListRow({required this.playLists});

  final List<PlayList> playLists;

  @override
  Widget build(BuildContext context) {
    if (playLists.isEmpty) {
      return const _EmptyHint(message: 'Aún no hay playlists');
    }
    return ListView.separated(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: playLists.length,
      separatorBuilder: (_, _) => const SizedBox(width: 12),
      itemBuilder: (context, index) {
        final playList = playLists[index];
        return SizedBox(
          width: 130,
          child: InkWell(
            borderRadius: BorderRadius.circular(12),
            onTap: () => context.go('/library/${playList.id}'),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CoverArt(seed: playList.id, icon: Icons.queue_music),
                const SizedBox(height: 8),
                Text(
                  playList.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _TrackList extends StatelessWidget {
  const _TrackList({required this.tracks, required this.onPlay});

  final List<Track> tracks;
  final void Function(Track track) onPlay;

  @override
  Widget build(BuildContext context) {
    if (tracks.isEmpty) {
      return const _EmptyHint(message: 'Aún no hay pistas');
    }
    return Column(
      children: [
        for (final track in tracks)
          ListTile(
            leading: SizedBox(
              width: 48,
              child: CoverArt(seed: track.id, borderRadius: 8),
            ),
            title: Text(track.title, maxLines: 1, overflow: TextOverflow.ellipsis),
            subtitle: const Text('Pista'),
            trailing: const Icon(Icons.play_arrow),
            onTap: () => onPlay(track),
          ),
      ],
    );
  }
}

class _EmptyHint extends StatelessWidget {
  const _EmptyHint({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Text(
        message,
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
      ),
    );
  }
}
