import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../models/dashboard_models.dart';

final dashboardServiceProvider = Provider<DashboardService>((ref) {
  return DashboardService(ref.read(dioProvider));
});

class DashboardService {
  final Dio _dio;

  DashboardService(this._dio);

  Future<DashboardSummary> getSummary({DateTime? from, DateTime? to}) async {
    try {
      final params = <String, dynamic>{};
      if (from != null) params['from'] = from.toIso8601String();
      if (to != null) params['to'] = to.toIso8601String();

      final response = await _dio.get(
        ApiEndpoints.dashboardSummary,
        queryParameters: params,
      );
      return DashboardSummary.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<List<RecentPayment>> getRecentPayments({int limit = 5}) async {
    try {
      final response = await _dio.get(
        ApiEndpoints.recentPayments,
        queryParameters: {'limit': limit},
      );
      return (response.data as List)
          .map((e) => RecentPayment.fromJson(e))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<OccupancyData> getOccupancy() async {
    try {
      final response = await _dio.get(ApiEndpoints.occupancy);
      return OccupancyData.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<List<RevenueTrend>> getRevenueTrend({DateTime? from, DateTime? to}) async {
    try {
      final params = <String, dynamic>{};
      if (from != null) params['from'] = from.toIso8601String();
      if (to != null) params['to'] = to.toIso8601String();

      final response = await _dio.get(
        ApiEndpoints.revenueTrend,
        queryParameters: params,
      );
      return (response.data as List)
          .map((e) => RevenueTrend.fromJson(e))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}
