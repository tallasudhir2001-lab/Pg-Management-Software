import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/auth_models.dart';
import '../services/auth_service.dart';

// Auth state
enum AuthStatus { initial, loading, authenticated, pgSelection, unauthenticated, error }

class AuthState {
  final AuthStatus status;
  final String? errorMessage;
  final List<PgOption>? pgOptions;
  final bool isAdmin;

  const AuthState({
    this.status = AuthStatus.initial,
    this.errorMessage,
    this.pgOptions,
    this.isAdmin = false,
  });

  AuthState copyWith({
    AuthStatus? status,
    String? errorMessage,
    List<PgOption>? pgOptions,
    bool? isAdmin,
  }) {
    return AuthState(
      status: status ?? this.status,
      errorMessage: errorMessage,
      pgOptions: pgOptions ?? this.pgOptions,
      isAdmin: isAdmin ?? this.isAdmin,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  final AuthService _authService;

  AuthNotifier(this._authService) : super(const AuthState());

  Future<void> checkAuth() async {
    final isLoggedIn = await _authService.isLoggedIn();
    state = AuthState(
      status: isLoggedIn ? AuthStatus.authenticated : AuthStatus.unauthenticated,
    );
  }

  Future<void> login(String usernameOrEmail, String password) async {
    state = state.copyWith(status: AuthStatus.loading);

    try {
      final response = await _authService.login(
        LoginRequest(userNameOrEmail: usernameOrEmail, password: password),
      );

      if (response.requiresPgSelection == true) {
        state = AuthState(
          status: AuthStatus.pgSelection,
          pgOptions: response.pgs,
        );
      } else {
        state = AuthState(
          status: AuthStatus.authenticated,
          isAdmin: response.isAdmin ?? false,
        );
      }
    } catch (e) {
      state = AuthState(
        status: AuthStatus.error,
        errorMessage: e.toString(),
      );
    }
  }

  Future<void> selectPg(String pgId) async {
    state = state.copyWith(status: AuthStatus.loading);

    try {
      await _authService.selectPg(pgId);
      state = const AuthState(status: AuthStatus.authenticated);
    } catch (e) {
      state = AuthState(
        status: AuthStatus.error,
        errorMessage: e.toString(),
      );
    }
  }

  Future<void> logout() async {
    await _authService.logout();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref.read(authServiceProvider));
});
