import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/models/paged_result.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../models/room_models.dart';

final roomServiceProvider = Provider<RoomService>((ref) {
  return RoomService(ref.read(dioProvider));
});

class RoomService {
  final Dio _dio;

  RoomService(this._dio);

  Future<PagedResult<RoomListItem>> getRooms({
    int page = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? ac,
  }) async {
    try {
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
      };
      if (search != null && search.isNotEmpty) params['search'] = search;
      if (status != null) params['status'] = status;
      if (ac != null) params['ac'] = ac;

      final response = await _dio.get(
        ApiEndpoints.rooms,
        queryParameters: params,
      );
      return PagedResult.fromJson(response.data, RoomListItem.fromJson);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  /// Get all rooms without pagination (for dropdowns)
  Future<List<RoomListItem>> getAllRooms() async {
    try {
      final response = await _dio.get(
        ApiEndpoints.rooms,
        queryParameters: {'page': 1, 'pageSize': 100},
      );
      final result = PagedResult.fromJson(response.data, RoomListItem.fromJson);
      return result.items;
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<RoomListItem> getRoomDetails(String roomId) async {
    try {
      final response = await _dio.get(ApiEndpoints.roomDetails(roomId));
      return RoomListItem.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<List<dynamic>> getTenantsInRoom(String roomId) async {
    try {
      final response = await _dio.get('${ApiEndpoints.roomDetails(roomId)}/tenants');
      return response.data as List;
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<String> createRoom(Map<String, dynamic> data) async {
    try {
      final response = await _dio.post(ApiEndpoints.addRoom, data: data);
      return response.data['roomId'] ?? '';
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}
