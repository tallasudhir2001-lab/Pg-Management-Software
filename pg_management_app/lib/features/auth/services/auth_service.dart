import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/token_storage.dart';
import '../models/auth_models.dart';

final authServiceProvider = Provider<AuthService>((ref) {
  return AuthService(ref.read(dioProvider), ref.read(tokenStorageProvider));
});

class AuthService {
  final Dio _dio;
  final TokenStorage _tokenStorage;

  AuthService(this._dio, this._tokenStorage);

  Future<LoginResponse> login(LoginRequest request) async {
    try {
      final response = await _dio.post(
        ApiEndpoints.login,
        data: request.toJson(),
      );
      final loginResponse = LoginResponse.fromJson(response.data);

      if (loginResponse.requiresPgSelection == true) {
        // Multi-PG user — save temp token
        if (loginResponse.tempToken != null) {
          await _tokenStorage.saveTempToken(loginResponse.tempToken!);
        }
      } else if (loginResponse.token != null) {
        // Direct login (admin or single-PG user)
        await _tokenStorage.saveTokens(
          accessToken: loginResponse.token!,
          refreshToken: loginResponse.refreshToken!,
        );
        if (loginResponse.isAdmin == true) {
          await _tokenStorage.saveIsAdmin(true);
        }
      }

      return loginResponse;
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<void> selectPg(String pgId) async {
    try {
      final tempToken = await _tokenStorage.getTempToken();
      final response = await _dio.post(
        ApiEndpoints.selectPg,
        data: SelectPgRequest(pgId: pgId).toJson(),
        options: Options(headers: {'Authorization': 'Bearer $tempToken'}),
      );

      await _tokenStorage.saveTokens(
        accessToken: response.data['token'],
        refreshToken: response.data['refreshToken'],
      );
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<void> logout() async {
    try {
      final refreshToken = await _tokenStorage.getRefreshToken();
      if (refreshToken != null) {
        await _dio.post(ApiEndpoints.logout, data: {
          'refreshToken': refreshToken,
        });
      }
    } catch (_) {
      // Ignore logout API errors
    } finally {
      await _tokenStorage.clearTokens();
    }
  }

  Future<bool> isLoggedIn() async {
    return _tokenStorage.hasToken();
  }
}
