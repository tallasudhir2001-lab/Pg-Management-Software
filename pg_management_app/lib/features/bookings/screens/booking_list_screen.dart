import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../../../core/router/app_routes.dart';
import '../providers/booking_provider.dart';

class BookingListScreen extends ConsumerWidget {
  const BookingListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(bookingListProvider);
    final dateFormat = DateFormat('dd MMM yyyy');
    final currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

    return Scaffold(
      appBar: AppBar(title: const Text('Bookings')),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result =
              await Navigator.pushNamed(context, AppRoutes.addBooking);
          if (result == true) {
            ref.read(bookingListProvider.notifier).refresh();
          }
        },
        child: const Icon(Icons.add),
      ),
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
                  onTap: () => ref.read(bookingListProvider.notifier).setStatusFilter(null),
                ),
                _FilterChip(
                  label: 'Active',
                  selected: state.statusFilter == 'Active',
                  onTap: () => ref.read(bookingListProvider.notifier).setStatusFilter('Active'),
                ),
                _FilterChip(
                  label: 'Cancelled',
                  selected: state.statusFilter == 'Cancelled',
                  onTap: () => ref.read(bookingListProvider.notifier).setStatusFilter('Cancelled'),
                ),
                _FilterChip(
                  label: 'Terminated',
                  selected: state.statusFilter == 'Terminated',
                  onTap: () => ref.read(bookingListProvider.notifier).setStatusFilter('Terminated'),
                ),
              ],
            ),
          ),
          Expanded(child: _buildBody(context, state, dateFormat, currencyFormat, ref)),
        ],
      ),
    );
  }

  Widget _buildBody(BuildContext context, BookingListState state,
      DateFormat dateFormat, NumberFormat currencyFormat, WidgetRef ref) {
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
              onPressed: () => ref.read(bookingListProvider.notifier).refresh(),
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }
    if (state.bookings.isEmpty) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.event_note, size: 64, color: AppColors.textSecondary),
            SizedBox(height: 16),
            Text('No bookings found'),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(bookingListProvider.notifier).refresh(),
      child: ListView.builder(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: state.bookings.length,
        itemBuilder: (context, index) {
          final booking = state.bookings[index];
          return GestureDetector(
            onTap: () {
              Navigator.pushNamed(
                context,
                AppRoutes.bookingDetail,
                arguments: booking.bookingId,
              );
            },
            child: Card(
            margin: const EdgeInsets.only(bottom: 10),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          booking.tenantName,
                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                        ),
                      ),
                      _StatusBadge(status: booking.status),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      const Icon(Icons.meeting_room, size: 16, color: AppColors.textSecondary),
                      const SizedBox(width: 4),
                      Text('Room ${booking.roomNumber}',
                          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                      const SizedBox(width: 16),
                      const Icon(Icons.calendar_today, size: 16, color: AppColors.textSecondary),
                      const SizedBox(width: 4),
                      Text(dateFormat.format(booking.scheduledCheckInDate),
                          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                    ],
                  ),
                  if (booking.advanceAmount > 0) ...[
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        const Icon(Icons.currency_rupee, size: 16, color: AppColors.success),
                        const SizedBox(width: 4),
                        Text(
                          'Advance: ${currencyFormat.format(booking.advanceAmount)}',
                          style: const TextStyle(fontSize: 13, color: AppColors.success, fontWeight: FontWeight.w500),
                        ),
                      ],
                    ),
                  ],
                  if (booking.tenantContact != null) ...[
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        const Icon(Icons.phone, size: 16, color: AppColors.textSecondary),
                        const SizedBox(width: 4),
                        Text(booking.tenantContact!,
                            style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ),
          );
        },
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;

  const _StatusBadge({required this.status});

  @override
  Widget build(BuildContext context) {
    Color color;
    switch (status) {
      case 'Active':
        color = AppColors.success;
        break;
      case 'Cancelled':
        color = AppColors.error;
        break;
      case 'Terminated':
        color = AppColors.textSecondary;
        break;
      default:
        color = AppColors.warning;
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        status,
        style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600),
      ),
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
