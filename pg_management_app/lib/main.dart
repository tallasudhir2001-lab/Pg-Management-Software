import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'core/constants/app_theme.dart';
import 'core/router/app_routes.dart';
import 'features/auth/providers/auth_provider.dart';
import 'features/auth/screens/login_screen.dart';
import 'features/auth/screens/pg_selection_screen.dart';
import 'core/shell_screen.dart';

void main() {
  runApp(const ProviderScope(child: PgManagementApp()));
}

class PgManagementApp extends ConsumerStatefulWidget {
  const PgManagementApp({super.key});

  @override
  ConsumerState<PgManagementApp> createState() => _PgManagementAppState();
}

class _PgManagementAppState extends ConsumerState<PgManagementApp> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(authProvider.notifier).checkAuth());
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authProvider);

    return MaterialApp(
      title: 'PG Management',
      theme: AppTheme.lightTheme,
      debugShowCheckedModeBanner: false,
      onGenerateRoute: AppRoutes.onGenerateRoute,
      home: _buildHome(authState),
    );
  }

  Widget _buildHome(AuthState authState) {
    switch (authState.status) {
      case AuthStatus.initial:
      case AuthStatus.loading:
        return const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        );
      case AuthStatus.authenticated:
        return const ShellScreen();
      case AuthStatus.pgSelection:
        return PgSelectionScreen(pgOptions: authState.pgOptions ?? []);
      case AuthStatus.unauthenticated:
      case AuthStatus.error:
        return const LoginScreen();
    }
  }
}
