import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../shared/widgets/cover_art.dart';
import '../application/player_controller.dart';

class MiniPlayer extends ConsumerWidget {
  const MiniPlayer({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(playerControllerProvider);
    if (!state.hasContent) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final controller = ref.read(playerControllerProvider.notifier);
    final track = state.track!;

    return Material(
      color: theme.colorScheme.surfaceContainerHigh,
      elevation: 3,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 6, 4, 2),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                SizedBox(
                  width: 40,
                  child: CoverArt(seed: track.id, borderRadius: 6),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        track.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.titleSmall,
                      ),
                      Text(
                        state.error ??
                            (state.loading ? 'Cargando…' : 'Reproduciendo'),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: state.error != null
                              ? theme.colorScheme.error
                              : theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  iconSize: 36,
                  icon: Icon(
                    state.playing ? Icons.pause_circle : Icons.play_circle,
                  ),
                  onPressed: state.error != null ? null : controller.togglePlay,
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: controller.stop,
                ),
              ],
            ),
            _ProgressBar(
              position: state.position,
              duration: state.duration,
              onSeek: controller.seek,
            ),
          ],
        ),
      ),
    );
  }
}

class _ProgressBar extends StatelessWidget {
  const _ProgressBar({
    required this.position,
    required this.duration,
    required this.onSeek,
  });

  final Duration position;
  final Duration duration;
  final void Function(Duration position) onSeek;

  @override
  Widget build(BuildContext context) {
    final totalMs = duration.inMilliseconds;
    final value = totalMs == 0
        ? 0.0
        : (position.inMilliseconds / totalMs).clamp(0.0, 1.0);

    return Row(
      children: [
        Text(_format(position), style: Theme.of(context).textTheme.labelSmall),
        Expanded(
          child: Slider(
            value: value,
            onChanged: totalMs == 0
                ? null
                : (fraction) => onSeek(
                      Duration(milliseconds: (fraction * totalMs).round()),
                    ),
          ),
        ),
        Text(_format(duration), style: Theme.of(context).textTheme.labelSmall),
      ],
    );
  }

  String _format(Duration value) {
    final minutes = value.inMinutes;
    final seconds = value.inSeconds % 60;
    return '$minutes:${seconds.toString().padLeft(2, '0')}';
  }
}
