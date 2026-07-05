import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth/auth_providers.dart';
import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/cover_art.dart';
import '../../playlists/data/playlist_repository.dart';
import '../../playlists/domain/playlist.dart';

class LibraryPage extends ConsumerWidget {
  const LibraryPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final userId = ref.watch(currentUserIdProvider);
    final provider = userId != null
        ? myPlayListsProvider(userId)
        : playListsProvider;
    final playLists = ref.watch(provider);
    final emptyMessage = userId != null
        ? 'Aún no tienes playlists'
        : 'No hay playlists públicas';

    return AsyncValueView<List<PlayList>>(
      value: playLists,
      onRetry: () => ref.invalidate(provider),
      data: (items) {
        if (items.isEmpty) {
          return Center(child: Text(emptyMessage));
        }
        return RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(provider);
            await ref.read(provider.future);
          },
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(vertical: 8),
            itemCount: items.length,
            itemBuilder: (context, index) {
              final playList = items[index];
              return ListTile(
                leading: SizedBox(
                  width: 48,
                  child: CoverArt(
                    seed: playList.id,
                    icon: Icons.queue_music,
                    borderRadius: 8,
                  ),
                ),
                title: Text(playList.name),
                subtitle: playList.description.isEmpty
                    ? null
                    : Text(
                        playList.description,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => context.go('/library/${playList.id}'),
              );
            },
          ),
        );
      },
    );
  }
}
