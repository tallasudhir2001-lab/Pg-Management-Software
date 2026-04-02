import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/models/paged_result.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../models/booking_models.dart';

final bookingServiceProvider = Provider<BookingService>((ref) {
  return BookingService(ref.read(dioProvider));
});

class BookingService {
  final Dio _dio;

  BookingService(this._dio);

  Future<PagedResult<BookingListItem>> getBookings({
    int page = 1,
    int pageSize = 10,
    String? status,
  }) async {
    try {
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        'sortDir': 'desc',
      };
      if (status != null) params['status'] = status;

      final response = await _dio.get(
        ApiEndpoints.bookings,
        queryParameters: params,
      );
      return PagedResult.fromJson(response.data, BookingListItem.fromJson);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}
