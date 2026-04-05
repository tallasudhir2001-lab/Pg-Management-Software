import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/room_models.dart';
import '../services/room_service.dart';

class RoomListState {
  final List<RoomListItem> rooms;
  final bool isLoading;
  final String? error;
  final int totalCount;
  final String? statusFilter;

  const RoomListState({
    this.rooms = const [],
    this.isLoading = false,
    this.error,
    this.totalCount = 0,
    this.statusFilter,
  });

  RoomListState copyWith({
    List<RoomListItem>? rooms,
    bool? isLoading,
    String? error,
    int? totalCount,
    String? statusFilter,
  }) {
    return RoomListState(
      rooms: rooms ?? this.rooms,
      isLoading: isLoading ?? this.isLoading,
      error: error,
      totalCount: totalCount ?? this.totalCount,
      statusFilter: statusFilter ?? this.statusFilter,
    );
  }
}

class RoomListNotifier extends StateNotifier<RoomListState> {
  final RoomService _service;

  RoomListNotifier(this._service) : super(const RoomListState()) {
    loadRooms();
  }

  Future<void> loadRooms() async {
    state = state.copyWith(isLoading: true);
    try {
      final result = await _service.getRooms(
        page: 1,
        pageSize: 50,
        status: state.statusFilter,
      );
      state = state.copyWith(
        rooms: result.items,
        isLoading: false,
        totalCount: result.totalCount,
      );
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  void setStatusFilter(String? status) {
    state = state.copyWith(statusFilter: status);
    loadRooms();
  }

  Future<void> refresh() async => loadRooms();
}

final roomListProvider =
    StateNotifierProvider.autoDispose<RoomListNotifier, RoomListState>((ref) {
  return RoomListNotifier(ref.read(roomServiceProvider));
});

// All rooms for dropdowns
final allRoomsProvider = FutureProvider<List<RoomListItem>>((ref) async {
  return ref.read(roomServiceProvider).getAllRooms();
});

// Single room details
final roomDetailsProvider =
    FutureProvider.autoDispose.family<RoomListItem, String>((ref, roomId) async {
  return ref.read(roomServiceProvider).getRoomDetails(roomId);
});

// Tenants in a room
final roomTenantsProvider =
    FutureProvider.autoDispose.family<List<dynamic>, String>((ref, roomId) async {
  return ref.read(roomServiceProvider).getTenantsInRoom(roomId);
});
