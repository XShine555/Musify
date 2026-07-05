import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/dio_client.dart';
import '../../../shared/models/paginated_response.dart';
import '../domain/track.dart';

class TrackRepository {
  TrackRepository(this._dio);

  final Dio _dio;

  Future<PaginatedResponse<Track>> getTracks({
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/tracks',
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return PaginatedResponse<Track>.fromJson(response.data!, Track.fromJson);
  }
}

final trackRepositoryProvider = Provider<TrackRepository>((ref) {
  return TrackRepository(ref.watch(dioProvider));
});

final tracksProvider = FutureProvider.autoDispose<List<Track>>((ref) async {
  final page = await ref.watch(trackRepositoryProvider).getTracks();
  return page.items;
});
