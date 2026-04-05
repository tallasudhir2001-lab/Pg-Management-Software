import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/expense_models.dart';
import '../services/expense_service.dart';

class ExpenseListState {
  final List<ExpenseListItem> expenses;
  final bool isLoading;
  final bool isLoadingMore;
  final String? error;
  final int currentPage;
  final int totalCount;
  final bool hasMore;

  const ExpenseListState({
    this.expenses = const [],
    this.isLoading = false,
    this.isLoadingMore = false,
    this.error,
    this.currentPage = 1,
    this.totalCount = 0,
    this.hasMore = true,
  });

  ExpenseListState copyWith({
    List<ExpenseListItem>? expenses,
    bool? isLoading,
    bool? isLoadingMore,
    String? error,
    int? currentPage,
    int? totalCount,
    bool? hasMore,
  }) {
    return ExpenseListState(
      expenses: expenses ?? this.expenses,
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      error: error,
      currentPage: currentPage ?? this.currentPage,
      totalCount: totalCount ?? this.totalCount,
      hasMore: hasMore ?? this.hasMore,
    );
  }
}

class ExpenseListNotifier extends StateNotifier<ExpenseListState> {
  final ExpenseService _service;
  static const _pageSize = 15;

  ExpenseListNotifier(this._service) : super(const ExpenseListState()) {
    loadExpenses();
  }

  Future<void> loadExpenses() async {
    state = state.copyWith(isLoading: true);
    try {
      final result = await _service.getExpenses(
        page: 1,
        pageSize: _pageSize,
      );
      state = state.copyWith(
        expenses: result.items,
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
      final result = await _service.getExpenses(
        page: nextPage,
        pageSize: _pageSize,
      );
      state = state.copyWith(
        expenses: [...state.expenses, ...result.items],
        isLoadingMore: false,
        currentPage: nextPage,
        hasMore: result.hasNextPage,
      );
    } catch (e) {
      state = state.copyWith(isLoadingMore: false, error: e.toString());
    }
  }

  Future<void> refresh() async {
    await loadExpenses();
  }
}

final expenseListProvider =
    StateNotifierProvider.autoDispose<ExpenseListNotifier, ExpenseListState>((ref) {
  return ExpenseListNotifier(ref.read(expenseServiceProvider));
});

final expenseCategoriesProvider =
    FutureProvider<List<ExpenseCategory>>((ref) async {
  return ref.read(expenseServiceProvider).getCategories();
});
