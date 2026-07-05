import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/cover_art.dart';
import '../../player/application/player_controller.dart';
import '../../tracks/domain/track.dart';
import '../data/playlist_repository.dart';
import '../domain/playlist.dart';

class PlayListDetailPage extends ConsumerWidget {
  const PlayListDetailPage({super.key, required this.playlistId});

  final String playlistId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final playList = ref.watch(playListByIdProvider(playlistId));
    final tracks = ref.watch(playListTracksProvider(playlistId));

    return Scaffold(
      body: AsyncValueView<PlayList>(
        value: playList,
        onRetry: () => ref.invalidate(playListByIdProvider(playlistId)),
        data: (playlist) => CustomScrollView(
          slivers: [
            SliverAppBar.large(
              title: Text(playlist.name),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SizedBox(
                      width: 120,
                      child: CoverArt(
                        seed: playlist.id,
                        icon: Icons.queue_music,
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            playlist.name,
                            style: Theme.of(context).textTheme.headlineSmall,
                          ),
                          const SizedBox(height: 8),
                          if (playlist.description.isNotEmpty)
                            Text(
                              playlist.description,
                              style: Theme.of(context).textTheme.bodyMedium,
                            ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            _TracksSliver(
              tracks: tracks,
              onRetry: () =>
                  ref.invalidate(playListTracksProvider(playlistId)),
              onPlay: (track) =>
                  ref.read(playerControllerProvider.notifier).play(track),
            ),
            const SliverToBoxAdapter(child: SizedBox(height: 24)),
          ],
        ),
      ),
    );
  }
}

class _TracksSliver extends StatelessWidget {
  const _TracksSliver({
    required this.tracks,
    required this.onRetry,
    required this.onPlay,
  });

  final AsyncValue<List<Track>> tracks;
  final VoidCallback onRetry;
  final void Function(Track track) onPlay;

  @override
  Widget build(BuildContext context) {
    return tracks.when(
      loading: () => const SliverToBoxAdapter(
        child: Padding(
          padding: EdgeInsets.all(32),
          child: Center(child: CircularProgressIndicator()),
        ),
      ),
      error: (error, _) => SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Center(child: Text('Error al cargar pistas: $error')),
        ),
      ),
      data: (items) {
        if (items.isEmpty) {
          return const SliverToBoxAdapter(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: Text('Esta playlist no tiene pistas')),
            ),
          );
        }
        return SliverList.builder(
          itemCount: items.length,
          itemBuilder: (context, index) {
            final track = items[index];
            return ListTile(
              leading: Text(
                '${index + 1}',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              title: Text(
                track.title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              trailing: const Icon(Icons.play_arrow),
              onTap: () => onPlay(track),
            );
          },
        );
      },
    );
  }
}
