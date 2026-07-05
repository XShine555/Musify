class TrackStream {
  const TrackStream({
    required this.manifestUrl,
    required this.ticket,
    required this.expiresInSeconds,
  });

  final String manifestUrl;
  final String ticket;
  final int expiresInSeconds;

  factory TrackStream.fromJson(Map<String, dynamic> json) {
    return TrackStream(
      manifestUrl: json['manifestUrl'] as String,
      ticket: json['ticket'] as String,
      expiresInSeconds: json['expiresInSeconds'] as int? ?? 0,
    );
  }
}
