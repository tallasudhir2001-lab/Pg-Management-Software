import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../providers/room_provider.dart';

class RoomDetailScreen extends ConsumerWidget {
  final String roomId;

  const RoomDetailScreen({super.key, required this.roomId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final roomAsync = ref.watch(roomDetailsProvider(roomId));
    final tenantsAsync = ref.watch(roomTenantsProvider(roomId));
    final currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

    return Scaffold(
      appBar: AppBar(title: const Text('Room Details')),
      body: roomAsync.when(
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
                onPressed: () => ref.invalidate(roomDetailsProvider(roomId)),
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
        data: (room) => SingleChildScrollView(
          padding: const EdgeInsets.all(AppSizes.paddingMd),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Room header
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    children: [
                      CircleAvatar(
                        radius: 40,
                        backgroundColor: _statusColor(room.status)
                            .withValues(alpha: 0.15),
                        child: Text(
                          room.roomNumber,
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                            color: _statusColor(room.status),
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        'Room ${room.roomNumber}',
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const SizedBox(height: 4),
                      _StatusBadge(status: room.status),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Room info
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Room Information',
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const Divider(),
                      _InfoRow(
                        icon: Icons.people,
                        label: 'Capacity',
                        value: '${room.capacity} beds',
                      ),
                      _InfoRow(
                        icon: Icons.person,
                        label: 'Occupied',
                        value: '${room.occupied} beds',
                      ),
                      _InfoRow(
                        icon: Icons.bed,
                        label: 'Vacant',
                        value: '${room.vacancies} beds',
                        valueColor: room.vacancies > 0
                            ? AppColors.success
                            : AppColors.error,
                      ),
                      _InfoRow(
                        icon: Icons.currency_rupee,
                        label: 'Rent',
                        value: currencyFormat.format(room.rentAmount),
                        valueColor: AppColors.primary,
                      ),
                      _InfoRow(
                        icon: Icons.ac_unit,
                        label: 'AC',
                        value: room.isAc ? 'Yes' : 'No',
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Occupancy bar
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Occupancy',
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const SizedBox(height: 12),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: LinearProgressIndicator(
                          value: room.capacity > 0
                              ? room.occupied / room.capacity
                              : 0,
                          minHeight: 12,
                          backgroundColor: AppColors.divider.withValues(alpha: 0.3),
                          color: _statusColor(room.status),
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '${room.occupied} of ${room.capacity} beds occupied',
                        style: const TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Current tenants
              tenantsAsync.when(
                loading: () => const Card(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ),
                error: (e, _) => const SizedBox.shrink(),
                data: (tenants) {
                  if (tenants.isEmpty) {
                    return Card(
                      child: Padding(
                        padding: const EdgeInsets.all(AppSizes.paddingMd),
                        child: Column(
                          children: [
                            const Icon(Icons.people_outline,
                                size: 48, color: AppColors.textSecondary),
                            const SizedBox(height: 8),
                            Text(
                              'No tenants in this room',
                              style: TextStyle(color: AppColors.textSecondary),
                            ),
                          ],
                        ),
                      ),
                    );
                  }
                  return Card(
                    child: Padding(
                      padding: const EdgeInsets.all(AppSizes.paddingMd),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Current Tenants',
                            style: Theme.of(context)
                                .textTheme
                                .titleSmall
                                ?.copyWith(fontWeight: FontWeight.bold),
                          ),
                          const Divider(),
                          ...tenants.map((t) {
                            final name = t['name'] ?? 'Unknown';
                            final contact = t['contactNumber'] ?? '';
                            return ListTile(
                              dense: true,
                              contentPadding: EdgeInsets.zero,
                              leading: CircleAvatar(
                                radius: 18,
                                child: Text(
                                  name.isNotEmpty
                                      ? name[0].toUpperCase()
                                      : '?',
                                ),
                              ),
                              title: Text(name),
                              subtitle:
                                  contact.isNotEmpty ? Text(contact) : null,
                            );
                          }),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'available':
        return AppColors.success;
      case 'partial':
        return AppColors.warning;
      case 'full':
        return AppColors.error;
      default:
        return AppColors.textSecondary;
    }
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;

  const _StatusBadge({required this.status});

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
