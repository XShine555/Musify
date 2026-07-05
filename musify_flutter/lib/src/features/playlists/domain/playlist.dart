class PlayList {
  const PlayList({
    required this.id,
    required this.name,
    required this.description,
    required this.smallImageKeyName,
    required this.mediumImageKeyName,
    required this.largeImageKeyName,
    required this.createdAt,
    required this.updatedAt,
  });

  final String id;
  final String name;
  final String description;
  final String smallImageKeyName;
  final String mediumImageKeyName;
  final String largeImageKeyName;
  final DateTime createdAt;
  final DateTime updatedAt;

  factory PlayList.fromJson(Map<String, dynamic> json) {
    return PlayList(
      id: json['id'] as String,
      name: json['name'] as String? ?? 'Sin nombre',
      description: json['description'] as String? ?? '',
      smallImageKeyName: json['smallImageKeyName'] as String? ?? '',
      mediumImageKeyName: json['mediumImageKeyName'] as String? ?? '',
      largeImageKeyName: json['largeImageKeyName'] as String? ?? '',
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: DateTime.parse(json['updatedAt'] as String),
    );
  }
}
