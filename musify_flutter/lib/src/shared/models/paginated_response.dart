class PaginatedResponse<T> {
  const PaginatedResponse({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.pageCount,
    required this.totalItemCount,
    required this.hasNextPage,
    required this.hasPreviousPage,
  });

  final List<T> items;
  final int pageNumber;
  final int pageSize;
  final int pageCount;
  final int totalItemCount;
  final bool hasNextPage;
  final bool hasPreviousPage;

  factory PaginatedResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> item) fromItem,
  ) {
    final rawItems = (json['items'] as List<dynamic>? ?? const []);
    return PaginatedResponse<T>(
      items: rawItems
          .map((dynamic e) => fromItem(e as Map<String, dynamic>))
          .toList(growable: false),
      pageNumber: json['pageNumber'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? rawItems.length,
      pageCount: json['pageCount'] as int? ?? 1,
      totalItemCount: json['totalItemCount'] as int? ?? rawItems.length,
      hasNextPage: json['hasNextPage'] as bool? ?? false,
      hasPreviousPage: json['hasPreviousPage'] as bool? ?? false,
    );
  }
}
