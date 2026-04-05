import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../providers/booking_provider.dart';
import '../services/booking_service.dart';

class BookingDetailScreen extends ConsumerWidget {
  final String bookingId;

  const BookingDetailScreen({super.key, required this.bookingId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bookingAsync = ref.watch(bookingDetailsProvider(bookingId));
    final dateFormat = DateFormat('dd MMM yyyy');
    final currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

    return Scaffold(
      appBar: AppBar(title: const Text('Booking Details')),
      body: bookingAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 48, color: AppColors.error),
              const SizedBox(height: 16),
              Text(e.toString()),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () =>
                    ref.invalidate(bookingDetailsProvider(bookingId)),
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
        data: (booking) => SingleChildScrollView(
          padding: const EdgeInsets.all(AppSizes.paddingMd),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    children: [
                      CircleAvatar(
                        radius: 40,
                        backgroundColor:
                            _statusColor(booking.status).withValues(alpha: 0.15),
                        child: Icon(
                          Icons.event_note,
                          size: 36,
                          color: _statusColor(booking.status),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        booking.tenantName,
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const SizedBox(height: 4),
                      _StatusBadge(status: booking.status),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Booking info
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Booking Information',
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const Divider(),
                      _InfoRow(
                        icon: Icons.meeting_room,
                        label: 'Room',
                        value: 'Room ${booking.roomNumber}',
                      ),
                      _InfoRow(
                        icon: Icons.calendar_today,
                        label: 'Check-in',
                        value: dateFormat.format(booking.scheduledCheckInDate),
                      ),
                      _InfoRow(
                        icon: Icons.access_time,
                        label: 'Booked On',
                        value: dateFormat.format(booking.createdAt),
                      ),
                      if (booking.createdBy.isNotEmpty)
                        _InfoRow(
                          icon: Icons.person_outline,
                          label: 'Booked By',
                          value: booking.createdBy,
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Tenant info
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Tenant Information',
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const Divider(),
                      _InfoRow(
                        icon: Icons.person,
                        label: 'Name',
                        value: booking.tenantName,
                      ),
                      _InfoRow(
                        icon: Icons.phone,
                        label: 'Contact',
                        value: booking.tenantContact,
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Advance info
              if (booking.advanceAmount > 0)
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSizes.paddingMd),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Advance Payment',
                          style: Theme.of(context)
                              .textTheme
                              .titleSmall
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                        const Divider(),
                        _InfoRow(
                          icon: Icons.currency_rupee,
                          label: 'Amount',
                          value: currencyFormat.format(booking.advanceAmount),
                          valueColor: AppColors.success,
                        ),
                      ],
                    ),
                  ),
                ),

              // Notes
              if (booking.notes != null && booking.notes!.isNotEmpty) ...[
                const SizedBox(height: 16),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSizes.paddingMd),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Notes',
                          style: Theme.of(context)
                              .textTheme
                              .titleSmall
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                        const Divider(),
                        Text(booking.notes!),
                      ],
                    ),
                  ),
                ),
              ],
              const SizedBox(height: 24),

              // Cancel button for active bookings
              if (booking.status == 'Active')
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: OutlinedButton.icon(
                    onPressed: () => _confirmCancel(context, ref, booking.bookingId),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.error,
                      side: const BorderSide(color: AppColors.error),
                    ),
                    icon: const Icon(Icons.cancel_outlined),
                    label: const Text('Cancel Booking'),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  void _confirmCancel(BuildContext context, WidgetRef ref, String bookingId) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel Booking?'),
        content: const Text('Are you sure you want to cancel this booking?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('No'),
          ),
          TextButton(
            onPressed: () async {
              Navigator.pop(ctx);
              try {
                await ref.read(bookingServiceProvider).cancelBooking(bookingId);
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Booking cancelled'),
                      backgroundColor: AppColors.success,
                    ),
                  );
                  ref.invalidate(bookingDetailsProvider(bookingId));
                  ref.invalidate(bookingListProvider);
                }
              } catch (e) {
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text('Error: $e'),
                      backgroundColor: AppColors.error,
                    ),
                  );
                }
              }
            },
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            child: const Text('Yes, Cancel'),
          ),
        ],
      ),
    );
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Active':
        return AppColors.success;
      case 'Cancelled':
        return AppColors.error;
      case 'Terminated':
        return AppColors.textSecondary;
      default:
        return AppColors.warning;
    }
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
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        status,
        style: TextStyle(
            color: color, fontSize: 12, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
    this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Icon(icon, size: 18, color: AppColors.textSecondary),
          const SizedBox(width: 12),
          SizedBox(
            width: 80,
            child: Text(
              label,
              style: const TextStyle(
                  color: AppColors.textSecondary, fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontWeight: FontWeight.w500,
                color: valueColor,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
