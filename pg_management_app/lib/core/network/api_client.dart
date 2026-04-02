import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../constants/api_endpoints.dart';
import 'token_storage.dart';
import 'certificate_bypass.dart';

final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(
    baseUrl: ApiEndpoints.baseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 15),
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  ));

  // Allow self-signed HTTPS certificates in development (mobile/desktop only)
  configureCertificateBypass(dio);

  dio.interceptors.add(AuthInterceptor(ref));
  dio.interceptors.add(LogInterceptor(
    requestBody: true,
    responseBody: true,
    logPrint: (obj) => print('[DIO] $obj'),
  ));

  return dio;
});

class AuthInterceptor extends Interceptor {
  final Ref _ref;

  AuthInterceptor(this._ref);

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final tokenStorage = _ref.read(tokenStorageProvider);
    final token = await tokenStorage.getAccessToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      final tokenStorage = _ref.read(tokenStorageProvider);
      final refreshToken = await tokenStorage.getRefreshToken();

      if (refreshToken != null) {
        try {
          // Try to refresh the token
          final dio = Dio(BaseOptions(baseUrl: ApiEndpoints.baseUrl));
          final response = await dio.post(ApiEndpoints.refresh, data: {
            'refreshToken': refreshToken,
          });

          final newToken = response.data['token'] as String;
          final newRefreshToken = response.data['refreshToken'] as String;

          await tokenStorage.saveTokens(
            accessToken: newToken,
            refreshToken: newRefreshToken,
          );

          // Retry the original request with new token
          err.requestOptions.headers['Authorization'] = 'Bearer $newToken';
          final retryResponse = await dio.fetch(err.requestOptions);
          return handler.resolve(retryResponse);
        } catch (e) {
          // Refresh failed — clear tokens
          await tokenStorage.clearTokens();
        }
      }
    }
    handler.next(err);
  }
}
