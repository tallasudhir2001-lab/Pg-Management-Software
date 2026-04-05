import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/models/paged_result.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../models/expense_models.dart';

final expenseServiceProvider = Provider<ExpenseService>((ref) {
  return ExpenseService(ref.read(dioProvider));
});

class ExpenseService {
  final Dio _dio;

  ExpenseService(this._dio);

  Future<PagedResult<ExpenseListItem>> getExpenses({
    int page = 1,
    int pageSize = 10,
    String sortBy = 'ExpenseDate',
    String sortDir = 'desc',
  }) async {
    try {
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        'sortBy': sortBy,
        'sortDir': sortDir,
      };

      final response = await _dio.get(
        ApiEndpoints.expenses,
        queryParameters: params,
      );
      return PagedResult.fromJson(response.data, ExpenseListItem.fromJson);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<List<ExpenseCategory>> getCategories() async {
    try {
      final response = await _dio.get(ApiEndpoints.expenseCategories);
      return (response.data as List)
          .map((e) => ExpenseCategory.fromJson(e))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<String> createExpense(CreateExpenseRequest request) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.createExpense,
        data: request.toJson(),
      );
      return response.data['id'] ?? '';
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}
