import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

final tokenStorageProvider = Provider<TokenStorage>((ref) => TokenStorage());

class TokenStorage {
  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _isAdminKey = 'is_admin';
  static const _tempTokenKey = 'temp_token';

  final _storage = const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );

  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    await _storage.write(key: _accessTokenKey, value: accessToken);
    await _storage.write(key: _refreshTokenKey, value: refreshToken);
  }

  Future<void> saveTempToken(String token) async {
    await _storage.write(key: _tempTokenKey, value: token);
  }

  Future<void> saveIsAdmin(bool isAdmin) async {
    await _storage.write(key: _isAdminKey, value: isAdmin.toString());
  }

  Future<String?> getAccessToken() async {
    return _storage.read(key: _accessTokenKey);
  }

  Future<String?> getRefreshToken() async {
    return _storage.read(key: _refreshTokenKey);
  }

  Future<String?> getTempToken() async {
    return _storage.read(key: _tempTokenKey);
  }

  Future<bool> getIsAdmin() async {
    final val = await _storage.read(key: _isAdminKey);
    return val == 'true';
  }

  Future<bool> hasToken() async {
    final token = await getAccessToken();
    return token != null && token.isNotEmpty;
  }

  Future<void> clearTokens() async {
    await _storage.deleteAll();
  }
}
