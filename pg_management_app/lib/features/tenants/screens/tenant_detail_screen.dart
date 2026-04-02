import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../providers/tenant_provider.dart';

class TenantDetailScreen extends ConsumerWidget {
  final String tenantId;

  const TenantDetailScreen({super.key, required this.tenantId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tenantAsync = ref.watch(tenantDetailsProvider(tenantId));
    final dateFormat = DateFormat('dd MMM yyyy');
    final currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tenant Details'),
      ),
      body: tenantAsync.when(
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
                onPressed: () => ref.invalidate(tenantDetailsProvider(tenantId)),
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
        data: (tenant) => SingleChildScrollView(
          padding: const EdgeInsets.all(AppSizes.paddingMd),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Tenant header card
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    children: [
                      CircleAvatar(
                        radius: 40,
                        backgroundColor: AppColors.primary.withValues(alpha: 0.15),
                        child: Text(
                          tenant.name.isNotEmpty ? tenant.name[0].toUpperCase() : '?',
                          style: const TextStyle(
                            fontSize: 32,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        tenant.name,
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      if (tenant.roomNumber != null) ...[
                        const SizedBox(height: 4),
                        Text(
                          'Room ${tenant.roomNumber}',
                          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                color: AppColors.textSecondary,
                              ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Contact info
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Contact Information',
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const Divider(),
                      _InfoRow(icon: Icons.phone, label: 'Phone', value: tenant.contactNumber ?? 'N/A'),
                      _InfoRow(icon: Icons.email, label: 'Email', value: tenant.email ?? 'N/A'),
                      _InfoRow(icon: Icons.credit_card, label: 'Aadhaar', value: tenant.aadharNumber ?? 'N/A'),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Stay Details
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSizes.paddingMd),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Stay Details',
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const Divider(),
                      _InfoRow(
                        icon: Icons.calendar_today,
                        label: 'From',
                        value: tenant.fromDate != null
                            ? dateFormat.format(tenant.fromDate!)
                            : 'N/A',
                      ),
                      _InfoRow(
                        icon: Icons.calendar_today,
                        label: 'To',
                        value: tenant.toDate != null
                            ? dateFormat.format(tenant.toDate!)
                            : 'Ongoing',
                      ),
                      _InfoRow(
                        icon: Icons.hotel,
                        label: 'Stay Type',
                        value: tenant.stayType ?? 'MONTHLY',
                      ),
                      if (tenant.rentAmount != null)
                        _InfoRow(
                          icon: Icons.currency_rupee,
                          label: 'Rent',
                          value: currencyFormat.format(tenant.rentAmount),
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Notes
              if (tenant.notes != null && tenant.notes!.isNotEmpty)
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSizes.paddingMd),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Notes',
                          style: Theme.of(context).textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.bold,
                              ),
                        ),
                        const Divider(),
                        Text(tenant.notes!),
                      ],
                    ),
                  ),
                ),

              // Recent Payments
              if (tenant.payments != null && tenant.payments!.isNotEmpty) ...[
                const SizedBox(height: 16),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSizes.paddingMd),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Recent Payments',
                          style: Theme.of(context).textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.bold,
                              ),
                        ),
                        const Divider(),
                        ...tenant.payments!.take(5).map((p) {
                          final amount = (p['amount'] as num?)?.toDouble() ?? 0;
                          final date = p['paymentDate'] != null
                              ? DateTime.parse(p['paymentDate'])
                              : null;
                          return ListTile(
                            dense: true,
                            contentPadding: EdgeInsets.zero,
                            leading: const Icon(Icons.payment, size: 20),
                            title: Text(currencyFormat.format(amount)),
                            subtitle: date != null
                                ? Text(dateFormat.format(date))
                                : null,
                            trailing: Text(
                              p['paymentModeCode'] ?? '',
                              style: const TextStyle(fontSize: 12),
                            ),
                          );
                        }),
                      ],
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
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
                color: AppColors.textSecondary,
                fontSize: 13,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
          ),
        ],
      ),
    );
  }
}
