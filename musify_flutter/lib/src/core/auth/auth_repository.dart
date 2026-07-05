import 'package:oidc/oidc.dart';
import 'package:oidc_default_store/oidc_default_store.dart';

import '../config/app_config.dart';

class AuthRepository {
  AuthRepository() : _manager = _createManager();

  final OidcUserManager _manager;
  bool _initialized = false;

  Future<void> init() async {
    if (_initialized) return;
    _initialized = true;
    await _manager.init();
  }

  Stream<OidcUser?> get userChanges => _manager.userChanges();

  OidcUser? get currentUser => _manager.currentUser;

  String? get accessToken => _manager.currentUser?.token.accessToken;

  Future<OidcUser?> login() => _manager.loginAuthorizationCodeFlow();

  Future<void> logout() => _manager.logout();

  Future<void> dispose() => _manager.dispose();

  static OidcUserManager _createManager() {
    final redirectUri = Uri.parse('http://localhost:$_redirectPort/');

    return OidcUserManager.lazy(
      discoveryDocumentUri: OidcUtils.getOpenIdConfigWellKnownUri(
        Uri.parse(AppConfig.zitadelIssuer),
      ),
      clientCredentials: const OidcClientAuthentication.none(
        clientId: AppConfig.zitadelClientId,
      ),
      store: OidcDefaultStore(),
      settings: OidcUserManagerSettings(
        scope: const ['openid', 'profile', 'email', 'offline_access'],
        redirectUri: redirectUri,
        postLogoutRedirectUri: redirectUri,
      ),
    );
  }

  static const _redirectPort = 8765;
}
