class AppConfig {
  const AppConfig._();

  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5111',
  );

  static const String streamingGatewayUrl = String.fromEnvironment(
    'STREAMING_GATEWAY_URL',
    defaultValue: 'http://localhost:8081',
  );

  static const String zitadelIssuer = String.fromEnvironment(
    'ZITADEL_ISSUER',
    defaultValue: 'http://localhost:8080',
  );

  static const String zitadelClientId = String.fromEnvironment(
    'ZITADEL_CLIENT_ID',
    defaultValue: '372825689755287555',
  );
}
