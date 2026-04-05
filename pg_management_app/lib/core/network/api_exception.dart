import 'package:dio/dio.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final dynamic data;

  ApiException({
    required this.message,
    this.statusCode,
    this.data,
  });

  factory ApiException.fromDioException(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return ApiException(
          message: 'Connection timed out. Please check your internet.',
          statusCode: error.response?.statusCode,
        );
      case DioExceptionType.badResponse:
        final statusCode = error.response?.statusCode;
        final data = error.response?.data;
        String message;

        if (data is String && data.isNotEmpty) {
          message = data;
        } else if (data is Map && data.containsKey('message')) {
          message = data['message'];
        } else if (data is Map && data.containsKey('title')) {
          message = data['title'];
        } else if (statusCode == 401) {
          message = 'Session expired. Please login again.';
        } else if (statusCode == 403) {
          message = 'You don\'t have permission for this action.';
        } else if (statusCode == 404) {
          message = 'Resource not found.';
        } else if (statusCode == 500) {
          message = 'Server error. Please try again later.';
        } else {
          message = 'Something went wrong.';
        }

        return ApiException(
          message: message,
          statusCode: statusCode,
          data: data,
        );
      case DioExceptionType.cancel:
        return ApiException(message: 'Request cancelled.');
      case DioExceptionType.connectionError:
        return ApiException(
          message: 'Cannot connect to server. Please check your connection.',
        );
      default:
        return ApiException(message: 'Something went wrong.');
    }
  }

  @override
  String toString() => message;
}
