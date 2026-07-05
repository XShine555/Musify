import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:oidc/oidc.dart';

import '../../../core/auth/auth_providers.dart';

class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authUserProvider);

    return authState.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text('Error de autenticación: $error'),
        ),
      ),
      data: (user) =>
          user == null ? const _GuestView() : _UserView(user: user),
    );
  }
}

class _GuestView extends ConsumerStatefulWidget {
  const _GuestView();

  @override
  ConsumerState<_GuestView> createState() => _GuestViewState();
}

class _GuestViewState extends ConsumerState<_GuestView> {
  bool _loading = false;

  Future<void> _login() async {
    setState(() => _loading = true);
    try {
      await ref.read(authRepositoryProvider).login();
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('No se pudo iniciar sesión: $error')),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircleAvatar(
              radius: 40,
              backgroundColor: theme.colorScheme.secondaryContainer,
              child: Icon(
                Icons.person,
                size: 40,
                color: theme.colorScheme.onSecondaryContainer,
              ),
            ),
            const SizedBox(height: 16),
            Text('Invitado', style: theme.textTheme.titleLarge),
            const SizedBox(height: 8),
            Text(
              'Inicia sesión con Zitadel para crear playlists,\n'
              'subir pistas y reproducir tu biblioteca.',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: _loading ? null : _login,
              icon: _loading
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.login),
              label: const Text('Iniciar sesión'),
            ),
          ],
        ),
      ),
    );
  }
}

class _UserView extends ConsumerWidget {
  const _UserView({required this.user});

  final OidcUser user;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final claims = user.aggregatedClaims;
    final name = (claims['name'] ?? claims['preferred_username']) as String?;
    final email = claims['email'] as String?;
    final picture = claims['picture'] as String?;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircleAvatar(
              radius: 44,
              backgroundColor: theme.colorScheme.secondaryContainer,
              foregroundImage: picture != null ? NetworkImage(picture) : null,
              child: Icon(
                Icons.person,
                size: 44,
                color: theme.colorScheme.onSecondaryContainer,
              ),
            ),
            const SizedBox(height: 16),
            Text(
              name ?? 'Usuario',
              style: theme.textTheme.titleLarge,
              textAlign: TextAlign.center,
            ),
            if (email != null) ...[
              const SizedBox(height: 4),
              Text(email, style: theme.textTheme.bodyMedium),
            ],
            const SizedBox(height: 8),
            Text(
              'ID: ${user.uid ?? '—'}',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 24),
            OutlinedButton.icon(
              onPressed: () => ref.read(authRepositoryProvider).logout(),
              icon: const Icon(Icons.logout),
              label: const Text('Cerrar sesión'),
            ),
          ],
        ),
      ),
    );
  }
}
