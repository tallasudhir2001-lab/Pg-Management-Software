import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/tenant_models.dart';
import '../services/tenant_service.dart';

// Tenant list state
class TenantListState {
  final List<TenantListItem> tenants;
  final bool isLoading;
  final bool isLoadingMore;
  final String? error;
  final int currentPage;
  final int totalCount;
  final bool hasMore;
  final String? searchQuery;
  final String? statusFilter;

  const TenantListState({
    this.tenants = const [],
    this.isLoading = false,
    this.isLoadingMore = false,
    this.error,
    this.currentPage = 1,
    this.totalCount = 0,
    this.hasMore = true,
    this.searchQuery,
    this.statusFilter,
  });

  TenantListState copyWith({
    List<TenantListItem>? tenants,
    bool? isLoading,
    bool? isLoadingMore,
    String? error,
    int? currentPage,
    int? totalCount,
    bool? hasMore,
    String? searchQuery,
    String? statusFilter,
  }) {
    return TenantListState(
      tenants: tenants ?? this.tenants,
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      error: error,
      currentPage: currentPage ?? this.currentPage,
      totalCount: totalCount ?? this.totalCount,
      hasMore: hasMore ?? this.hasMore,
      searchQuery: searchQuery ?? this.searchQuery,
      statusFilter: statusFilter ?? this.statusFilter,
    );
  }
}

class TenantListNotifier extends StateNotifier<TenantListState> {
  final TenantService _service;
  static const _pageSize = 15;

  TenantListNotifier(this._service) : super(const TenantListState()) {
    loadTenants();
  }

  Future<void> loadTenants() async {
    state = state.copyWith(isLoading: true);
    try {
      final result = await _service.getTenants(
        page: 1,
        pageSize: _pageSize,
        search: state.searchQuery,
        status: state.statusFilter,
      );
      state = state.copyWith(
        tenants: result.items,
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
      final result = await _service.getTenants(
        page: nextPage,
        pageSize: _pageSize,
        search: state.searchQuery,
        status: state.statusFilter,
      );
      state = state.copyWith(
        tenants: [...state.tenants, ...result.items],
        isLoadingMore: false,
        currentPage: nextPage,
        hasMore: result.hasNextPage,
      );
    } catch (e) {
      state = state.copyWith(isLoadingMore: false, error: e.toString());
    }
  }

  void setSearch(String query) {
    state = state.copyWith(searchQuery: query.isEmpty ? null : query);
    loadTenants();
  }

  void setStatusFilter(String? status) {
    state = state.copyWith(statusFilter: status);
    loadTenants();
  }

  Future<void> refresh() async {
    await loadTenants();
  }
}

final tenantListProvider =
    StateNotifierProvider.autoDispose<TenantListNotifier, TenantListState>((ref) {
  return TenantListNotifier(ref.read(tenantServiceProvider));
});

// Single tenant details
final tenantDetailsProvider =
    FutureProvider.autoDispose.family<TenantDetails, String>((ref, tenantId) async {
  final service = ref.read(tenantServiceProvider);
  return service.getTenantDetails(tenantId);
});
