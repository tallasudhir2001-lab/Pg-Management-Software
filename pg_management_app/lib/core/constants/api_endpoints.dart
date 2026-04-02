class ApiEndpoints {
  // For Chrome/web testing: use localhost directly
  // For Android emulator: use https://10.0.2.2:7073/api
  // For physical device: use your PC's local IP (e.g. https://192.168.x.x:7073/api)
  static const String baseUrl = 'https://localhost:7073/api';

  // Auth
  static const String login = '/auth/login';
  static const String selectPg = '/auth/select-pg';
  static const String refresh = '/auth/refresh';
  static const String logout = '/auth/logout';

  // Dashboard
  static const String dashboardSummary = '/dashboard/summary';
  static const String revenueTrend = '/dashboard/revenue-trend';
  static const String recentPayments = '/dashboard/recent-payments';
  static const String occupancy = '/dashboard/occupancy';
  static const String expensesSummary = '/dashboard/expenses-summary';

  // Tenants
  static const String tenants = '/tenants';
  static const String createTenant = '/tenants/create-tenant';
  static String tenantDetails(String id) => '/tenants/$id';
  static String moveOut(String id) => '/tenants/$id/move-out';
  static String changeRoom(String id) => '/tenants/$id/change-room';

  // Rooms
  static const String rooms = '/rooms';
  static const String addRoom = '/rooms/add-room';
  static String roomDetails(String id) => '/rooms/$id';
  static String updateRoomRent(String id) => '/rooms/$id/rent';

  // Payments
  static const String payments = '/payments/history';
  static String paymentDetails(String id) => '/payments/$id';
  static const String createPayment = '/payments/create-payment';
  static String pendingRent(String tenantId) => '/payments/pending/$tenantId';
  static String sendReceipt(String id) => '/payments/$id/send-receipt';
  static String sendReceiptWhatsapp(String id) => '/payments/$id/send-receipt-whatsapp';

  // Payment Modes & Types
  static const String paymentModes = '/payment-modes';
  static const String paymentTypes = '/payment-types';

  // Expenses
  static const String expenses = '/expenses';
  static const String createExpense = '/expenses/create-expense';
  static const String expenseCategories = '/expense-categories';

  // Advances
  static String tenantAdvances(String tenantId) => '/advances/tenant/$tenantId';
  static const String createAdvance = '/advances';
  static String settleAdvance(String id) => '/advances/$id/settle';

  // Bookings
  static const String bookings = '/bookings';
  static const String createBooking = '/bookings/create-booking';

  // Reports
  static const String reports = '/reports';

  // Settings
  static const String notificationSettings = '/settings/notifications';
}
