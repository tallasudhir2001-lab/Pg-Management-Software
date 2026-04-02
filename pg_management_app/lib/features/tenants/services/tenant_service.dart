import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/models/paged_result.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../models/tenant_models.dart';
import '../models/create_tenant_models.dart';

final tenantServiceProvider = Provider<TenantService>((ref) {
  return TenantService(ref.read(dioProvider));
});

class TenantService {
  final Dio _dio;

  TenantService(this._dio);

  Future<PagedResult<TenantListItem>> getTenants({
    int page = 1,
    int pageSize = 10,
    String? search,
    String? status,
    String? roomId,
    bool? rentPending,
    String? sortBy,
    String? sortDir,
  }) async {
    try {
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
      };
      if (search != null && search.isNotEmpty) params['search'] = search;
      if (status != null) params['status'] = status;
      if (roomId != null) params['roomId'] = roomId;
      if (rentPending != null) params['rentPending'] = rentPending;
      if (sortBy != null) params['sortBy'] = sortBy;
      if (sortDir != null) params['sortDir'] = sortDir;

      final response = await _dio.get(
        ApiEndpoints.tenants,
        queryParameters: params,
      );
      return PagedResult.fromJson(response.data, TenantListItem.fromJson);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<TenantDetails> getTenantDetails(String tenantId) async {
    try {
      final response = await _dio.get(ApiEndpoints.tenantDetails(tenantId));
      return TenantDetails.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<String> createTenant(CreateTenantRequest request) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.createTenant,
        data: request.toJson(),
      );
      return response.data['tenantId'] ?? '';
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}
