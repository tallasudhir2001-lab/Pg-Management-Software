import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/dashboard_models.dart';
import '../services/dashboard_service.dart';

// Dashboard summary
final dashboardSummaryProvider = FutureProvider.autoDispose<DashboardSummary>((ref) async {
  final service = ref.read(dashboardServiceProvider);
  return service.getSummary();
});

// Recent payments
final recentPaymentsProvider = FutureProvider.autoDispose<List<RecentPayment>>((ref) async {
  final service = ref.read(dashboardServiceProvider);
  return service.getRecentPayments(limit: 5);
});

// Occupancy
final occupancyProvider = FutureProvider.autoDispose<OccupancyData>((ref) async {
  final service = ref.read(dashboardServiceProvider);
  return service.getOccupancy();
});

// Revenue trend
final revenueTrendProvider = FutureProvider.autoDispose<List<RevenueTrend>>((ref) async {
  final service = ref.read(dashboardServiceProvider);
  return service.getRevenueTrend();
});
