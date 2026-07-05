import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/dio_client.dart';
import '../../../shared/models/paginated_response.dart';
import '../../tracks/domain/track.dart';
import '../domain/playlist.dart';

class PlayListRepository {
  PlayListRepository(this._dio);

  final Dio _dio;

  Future<PaginatedResponse<PlayList>> getPlayLists({
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/playLists',
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return PaginatedResponse<PlayList>.fromJson(
      response.data!,
      PlayList.fromJson,
    );
  }

  Future<PaginatedResponse<PlayList>> getPlayListsByUser(
    String userId, {
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/playLists/users/$userId',
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return PaginatedResponse<PlayList>.fromJson(
      response.data!,
      PlayList.fromJson,
    );
  }

  Future<PlayList> getPlayListById(String id) async {
    final response = await _dio.get<Map<String, dynamic>>('/playLists/$id');
    return PlayList.fromJson(response.data!);
  }

  Future<PaginatedResponse<Track>> getPlayListTracks(
    String playlistId, {
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/playLists/$playlistId/tracks',
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return PaginatedResponse<Track>.fromJson(response.data!, Track.fromJson);
  }
}

final playListRepositoryProvider = Provider<PlayListRepository>((ref) {
  return PlayListRepository(ref.watch(dioProvider));
});

final playListsProvider =
    FutureProvider.autoDispose<List<PlayList>>((ref) async {
  final page = await ref.watch(playListRepositoryProvider).getPlayLists();
  return page.items;
});

final myPlayListsProvider =
    FutureProvider.autoDispose.family<List<PlayList>, String>((ref, userId) async {
  final page =
      await ref.watch(playListRepositoryProvider).getPlayListsByUser(userId);
  return page.items;
});

final playListByIdProvider =
    FutureProvider.autoDispose.family<PlayList, String>((ref, id) async {
  return ref.watch(playListRepositoryProvider).getPlayListById(id);
});

final playListTracksProvider =
    FutureProvider.autoDispose.family<List<Track>, String>((ref, id) async {
  final page = await ref.watch(playListRepositoryProvider).getPlayListTracks(id);
  return page.items;
});
