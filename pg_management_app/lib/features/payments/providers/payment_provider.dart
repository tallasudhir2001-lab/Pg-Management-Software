import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/payment_models.dart';
import '../services/payment_service.dart';

// Payment list state
class PaymentListState {
  final List<PaymentListItem> payments;
  final bool isLoading;
  final bool isLoadingMore;
  final String? error;
  final int currentPage;
  final int totalCount;
  final bool hasMore;

  const PaymentListState({
    this.payments = const [],
    this.isLoading = false,
    this.isLoadingMore = false,
    this.error,
    this.currentPage = 1,
    this.totalCount = 0,
    this.hasMore = true,
  });

  PaymentListState copyWith({
    List<PaymentListItem>? payments,
    bool? isLoading,
    bool? isLoadingMore,
    String? error,
    int? currentPage,
    int? totalCount,
    bool? hasMore,
  }) {
    return PaymentListState(
      payments: payments ?? this.payments,
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      error: error,
      currentPage: currentPage ?? this.currentPage,
      totalCount: totalCount ?? this.totalCount,
      hasMore: hasMore ?? this.hasMore,
    );
  }
}

class PaymentListNotifier extends StateNotifier<PaymentListState> {
  final PaymentService _service;
  static const _pageSize = 15;

  PaymentListNotifier(this._service) : super(const PaymentListState()) {
    loadPayments();
  }

  Future<void> loadPayments() async {
    state = state.copyWith(isLoading: true);
    try {
      final result = await _service.getPayments(
        page: 1,
        pageSize: _pageSize,
      );
      state = state.copyWith(
        payments: result.items,
        isLoading: false,
        currentPage: 1,
        totalCount: result.totalCount,
        hasMore: result.hasNextPage,
      );
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  Future<void> loadMore() async {
    if (state.isLoadingMore || !state.hasMore) return;

    state = state.copyWith(isLoadingMore: true);
    try {
      final nextPage = state.currentPage + 1;
      final result = await _service.getPayments(
        page: nextPage,
        pageSize: _pageSize,
      );
      state = state.copyWith(
        payments: [...state.payments, ...result.items],
        isLoadingMore: false,
        currentPage: nextPage,
        hasMore: result.hasNextPage,
      );
    } catch (e) {
      state = state.copyWith(isLoadingMore: false, error: e.toString());
    }
  }

  Future<void> refresh() async {
    await loadPayments();
  }
}

final paymentListProvider =
    StateNotifierProvider.autoDispose<PaymentListNotifier, PaymentListState>((ref) {
  return PaymentListNotifier(ref.read(paymentServiceProvider));
});

// Payment modes
final paymentModesProvider = FutureProvider<List<PaymentMode>>((ref) async {
  return ref.read(paymentServiceProvider).getPaymentModes();
});
