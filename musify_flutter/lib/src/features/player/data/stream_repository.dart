import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/dio_client.dart';
import '../domain/track_stream.dart';

class StreamRepository {
  StreamRepository(this._dio);

  final Dio _dio;

  Future<TrackStream> getStream(String trackId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/tracks/$trackId/stream',
    );
    return TrackStream.fromJson(response.data!);
  }
}

final streamRepositoryProvider = Provider<StreamRepository>((ref) {
  return StreamRepository(ref.watch(dioProvider));
});
