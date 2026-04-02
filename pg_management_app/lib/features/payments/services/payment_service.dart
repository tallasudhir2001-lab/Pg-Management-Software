import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/models/paged_result.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../models/payment_models.dart';
import '../models/pending_rent_models.dart';

final paymentServiceProvider = Provider<PaymentService>((ref) {
  return PaymentService(ref.read(dioProvider));
});

class PaymentService {
  final Dio _dio;

  PaymentService(this._dio);

  Future<PagedResult<PaymentListItem>> getPayments({
    int page = 1,
    int pageSize = 10,
    String? tenantId,
    String? search,
    String? mode,
    String? types,
    String sortBy = 'paymentDate',
    String sortDir = 'desc',
  }) async {
    try {
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        'sortBy': sortBy,
        'sortDir': sortDir,
      };
      if (tenantId != null) params['tenantId'] = tenantId;
      if (search != null) params['search'] = search;
      if (mode != null) params['mode'] = mode;
      if (types != null) params['types'] = types;

      final response = await _dio.get(
        ApiEndpoints.payments,
        queryParameters: params,
      );
      return PagedResult.fromJson(response.data, PaymentListItem.fromJson);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<List<PaymentMode>> getPaymentModes() async {
    try {
      final response = await _dio.get(ApiEndpoints.paymentModes);
      return (response.data as List)
          .map((e) => PaymentMode.fromJson(e))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<List<PaymentType>> getPaymentTypes() async {
    try {
      final response = await _dio.get(ApiEndpoints.paymentTypes);
      return (response.data as List)
          .map((e) => PaymentType.fromJson(e))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<void> sendReceipt(String paymentId, String email) async {
    try {
      await _dio.post(
        ApiEndpoints.sendReceipt(paymentId),
        data: {'recipientEmail': email},
      );
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<void> sendReceiptWhatsapp(String paymentId) async {
    try {
      await _dio.post(ApiEndpoints.sendReceiptWhatsapp(paymentId));
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<PendingRentResponse> getPendingRent(String tenantId) async {
    try {
      final response = await _dio.get(ApiEndpoints.pendingRent(tenantId));
      return PendingRentResponse.fromJson(response.data);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<String> createPayment(dynamic request) async {
    try {
      final data = request is Map ? request : request.toJson();
      final response = await _dio.post(
        ApiEndpoints.createPayment,
        data: data,
      );
      return response.data['paymentId'] ?? '';
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}
