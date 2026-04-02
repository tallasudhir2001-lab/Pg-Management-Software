import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../providers/room_provider.dart';
import '../models/room_models.dart';

class RoomListScreen extends ConsumerWidget {
  const RoomListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(roomListProvider);
    final currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

    return Scaffold(
      appBar: AppBar(title: const Text('Rooms')),
      body: Column(
        children: [
          // Filter chips
          SizedBox(
            height: 50,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              children: [
                _FilterChip(
                  label: 'All',
                  selected: state.statusFilter == null,
                  onTap: () => ref.read(roomListProvider.notifier).setStatusFilter(null),
                ),
                _FilterChip(
                  label: 'Available',
                  selected: state.statusFilter == 'available',
                  onTap: () => ref.read(roomListProvider.notifier).setStatusFilter('available'),
                ),
                _FilterChip(
                  label: 'Partial',
                  selected: state.statusFilter == 'partial',
                  onTap: () => ref.read(roomListProvider.notifier).setStatusFilter('partial'),
                ),
                _FilterChip(
                  label: 'Full',
                  selected: state.statusFilter == 'full',
                  onTap: () => ref.read(roomListProvider.notifier).setStatusFilter('full'),
                ),
              ],
            ),
          ),

          // Room count
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            child: Row(
              children: [
                Text(
                  '${state.totalCount} room${state.totalCount != 1 ? 's' : ''}',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: AppColors.textSecondary,
                      ),
                ),
              ],
            ),
          ),

          // Room grid
          Expanded(child: _buildBody(context, state, currencyFormat, ref)),
        ],
      ),
    );
  }

  Widget _buildBody(BuildContext context, RoomListState state,
      NumberFormat fmt, WidgetRef ref) {
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 48, color: AppColors.error),
            const SizedBox(height: 16),
            Text(state.error!),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () => ref.read(roomListProvider.notifier).refresh(),
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }
    if (state.rooms.isEmpty) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.meeting_room_outlined, size: 64, color: AppColors.textSecondary),
            SizedBox(height: 16),
            Text('No rooms found'),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(roomListProvider.notifier).refresh(),
      child: GridView.builder(
        padding: const EdgeInsets.all(16),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: 0.85,
        ),
        itemCount: state.rooms.length,
        itemBuilder: (context, index) {
          final room = state.rooms[index];
          return _RoomCard(room: room, currencyFormat: fmt);
        },
      ),
    );
  }
}

class _RoomCard extends StatelessWidget {
  final RoomListItem room;
  final NumberFormat currencyFormat;

  const _RoomCard({required this.room, required this.currencyFormat});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Room number & status
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Room ${room.roomNumber}',
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
                _StatusDot(status: room.status),
              ],
            ),
            const SizedBox(height: 8),

            // AC badge
            if (room.isAc)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: Colors.blue.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: const Text(
                  'AC',
                  style: TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: Colors.blue),
                ),
              ),
            const Spacer(),

            // Occupancy
            Row(
              children: [
                const Icon(Icons.people, size: 16, color: AppColors.textSecondary),
                const SizedBox(width: 4),
                Text(
                  '${room.occupied}/${room.capacity}',
                  style: const TextStyle(fontSize: 13),
                ),
              ],
            ),
            const SizedBox(height: 4),

            // Vacancies
            Row(
              children: [
                Icon(
                  Icons.bed,
                  size: 16,
                  color: room.vacancies > 0 ? AppColors.success : AppColors.error,
                ),
                const SizedBox(width: 4),
                Text(
                  '${room.vacancies} vacant',
                  style: TextStyle(
                    fontSize: 13,
                    color: room.vacancies > 0 ? AppColors.success : AppColors.error,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),

            // Rent
            Text(
              currencyFormat.format(room.rentAmount),
              style: const TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 14,
                color: AppColors.primary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusDot extends StatelessWidget {
  final String status;

  const _StatusDot({required this.status});

  @override
  Widget build(BuildContext context) {
    Color color;
    switch (status.toLowerCase()) {
      case 'available':
        color = AppColors.success;
        break;
      case 'partial':
        color = AppColors.warning;
        break;
      case 'full':
        color = AppColors.error;
        break;
      default:
        color = AppColors.textSecondary;
    }
    return Container(
      width: 10,
      height: 10,
      decoration: BoxDecoration(shape: BoxShape.circle, color: color),
    );
  }
}

class _FilterChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _FilterChip({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ChoiceChip(
        label: Text(label),
        selected: selected,
        onSelected: (_) => onTap(),
        selectedColor: AppColors.primary.withValues(alpha: 0.15),
        labelStyle: TextStyle(
          color: selected ? AppColors.primary : AppColors.textSecondary,
          fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
          fontSize: 13,
        ),
      ),
    );
  }
}
