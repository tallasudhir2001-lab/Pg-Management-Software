import 'package:flutter/material.dart';
import '../../features/auth/screens/login_screen.dart';
import '../../features/auth/screens/pg_selection_screen.dart';
import '../../features/tenants/screens/tenant_detail_screen.dart';
import '../../features/tenants/screens/add_tenant_screen.dart';
import '../../features/payments/screens/add_payment_screen.dart';
import '../../features/payments/screens/payment_detail_screen.dart';
import '../../features/rooms/screens/room_detail_screen.dart';
import '../../features/rooms/screens/add_room_screen.dart';
import '../../features/bookings/screens/booking_list_screen.dart';
import '../../features/bookings/screens/booking_detail_screen.dart';
import '../../features/bookings/screens/add_booking_screen.dart';
import '../../features/expenses/screens/expense_list_screen.dart';
import '../../features/expenses/screens/add_expense_screen.dart';
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
  static const String roomDetail = '/rooms/detail';
  static const String addRoom = '/rooms/add';
  static const String payments = '/payments';
  static const String paymentDetail = '/payments/detail';
  static const String addPayment = '/payments/add';
  static const String bookings = '/bookings';
  static const String bookingDetail = '/bookings/detail';
  static const String addBooking = '/bookings/add';
  static const String expenses = '/expenses';
  static const String addExpense = '/expenses/add';

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

      case roomDetail:
        final roomId = settings.arguments as String;
        return MaterialPageRoute(
          builder: (_) => RoomDetailScreen(roomId: roomId),
        );

      case addRoom:
        return MaterialPageRoute(
          builder: (_) => const AddRoomScreen(),
        );

      case payments:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 3),
        );

      case paymentDetail:
        final paymentId = settings.arguments as String;
        return MaterialPageRoute(
          builder: (_) => PaymentDetailScreen(paymentId: paymentId),
        );

      case addPayment:
        return MaterialPageRoute(
          builder: (_) => const AddPaymentScreen(),
        );

      case bookings:
        return MaterialPageRoute(
          builder: (_) => const BookingListScreen(),
        );

      case bookingDetail:
        final bookingId = settings.arguments as String;
        return MaterialPageRoute(
          builder: (_) => BookingDetailScreen(bookingId: bookingId),
        );

      case addBooking:
        return MaterialPageRoute(
          builder: (_) => const AddBookingScreen(),
        );

      case expenses:
        return MaterialPageRoute(
          builder: (_) => const ExpenseListScreen(),
        );

      case addExpense:
        return MaterialPageRoute(
          builder: (_) => const AddExpenseScreen(),
        );

      default:
        return MaterialPageRoute(
          builder: (_) => const ShellScreen(initialIndex: 0),
        );
    }
  }
}
