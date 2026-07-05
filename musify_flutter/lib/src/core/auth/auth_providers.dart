import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:oidc/oidc.dart';

import 'auth_repository.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  final repository = AuthRepository();
  ref.onDispose(repository.dispose);
  return repository;
});

final authUserProvider = StreamProvider<OidcUser?>((ref) async* {
  final repository = ref.watch(authRepositoryProvider);
  await repository.init();
  yield* repository.userChanges;
});

final isAuthenticatedProvider = Provider<bool>((ref) {
  return ref.watch(authUserProvider).valueOrNull != null;
});

final currentUserIdProvider = Provider<String?>((ref) {
  return ref.watch(authUserProvider).valueOrNull?.uid;
});
