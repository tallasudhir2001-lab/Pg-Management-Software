import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/booking_models.dart';
import '../services/booking_service.dart';

class BookingListState {
  final List<BookingListItem> bookings;
  final bool isLoading;
  final String? error;
  final int totalCount;
  final String? statusFilter;

  const BookingListState({
    this.bookings = const [],
    this.isLoading = false,
    this.error,
    this.totalCount = 0,
    this.statusFilter,
  });

  BookingListState copyWith({
    List<BookingListItem>? bookings,
    bool? isLoading,
    String? error,
    int? totalCount,
    String? statusFilter,
  }) {
    return BookingListState(
      bookings: bookings ?? this.bookings,
      isLoading: isLoading ?? this.isLoading,
      error: error,
      totalCount: totalCount ?? this.totalCount,
      statusFilter: statusFilter ?? this.statusFilter,
    );
  }
}

class BookingListNotifier extends StateNotifier<BookingListState> {
  final BookingService _service;

  BookingListNotifier(this._service) : super(const BookingListState()) {
    load();
  }

  Future<void> load() async {
    state = state.copyWith(isLoading: true);
    try {
      final result = await _service.getBookings(
        page: 1,
        pageSize: 50,
        status: state.statusFilter,
      );
      state = state.copyWith(
        bookings: result.items,
        isLoading: false,
        totalCount: result.totalCount,
      );
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  void setStatusFilter(String? status) {
    state = state.copyWith(statusFilter: status);
    load();
  }

  Future<void> refresh() async => load();
}

final bookingListProvider =
    StateNotifierProvider.autoDispose<BookingListNotifier, BookingListState>((ref) {
  return BookingListNotifier(ref.read(bookingServiceProvider));
});
