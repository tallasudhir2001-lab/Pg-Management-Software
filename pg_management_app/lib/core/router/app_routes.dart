import 'package:flutter/material.dart';
import '../../features/auth/screens/login_screen.dart';
import '../../features/auth/screens/pg_selection_screen.dart';
import '../../features/tenants/screens/tenant_detail_screen.dart';
import '../../features/tenants/screens/add_tenant_screen.dart';
import '../../features/payments/screens/add_payment_screen.dart';
import '../../features/auth/models/auth_models.dart';
import '../shell_screen.dart';

class AppRoutes {
  static const String login = '/login';
  static const String pgSelection = '/pg-selection';
  static const String dashboard = '/dashboard';
  static const String tenants = '/tenants';
  static const String tenantDetail = '/tenants/detail';
  static const String addTenant = '/tenants/add';
  static const String rooms = '/rooms';
  static const String payments = '/payments';
  static const String addPayment = '/payments/add';
  static const String bookings = '/bookings';

  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    switch (settings.name) {
      case login:
        return MaterialPageRoute(builder: (_) => const LoginScreen());

      case pgSelection:
        final pgOptions = settings.arguments as List<PgOption>;
        return MaterialPageRoute(
          builder: (_) => PgSelectionScreen(pgOptions: pgOptions),
        );

      case dashboard:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 0),
        );

      case tenants:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 1),
        );

      case tenantDetail:
        final tenantId = settings.arguments as String;
        return MaterialPageRoute(
          builder: (_) => TenantDetailScreen(tenantId: tenantId),
        );

      case addTenant:
        return MaterialPageRoute(
          builder: (_) => const AddTenantScreen(),
        );

      case rooms:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 2),
        );

      case payments:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 3),
        );

      case addPayment:
        return MaterialPageRoute(
          builder: (_) => const AddPaymentScreen(),
        );

      case bookings:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 4),
        );

      default:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 0),
        );
    }
  }
}
