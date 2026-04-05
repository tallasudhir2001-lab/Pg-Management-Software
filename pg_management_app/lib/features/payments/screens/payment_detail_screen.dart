import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../providers/payment_provider.dart';
import '../services/payment_service.dart';

class PaymentDetailScreen extends ConsumerWidget {
  final String paymentId;

  const PaymentDetailScreen({super.key, required this.paymentId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final paymentAsync = ref.watch(paymentDetailsProvider(paymentId));
    final dateFormat = DateFormat('dd MMM yyyy');
    final currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

    return Scaffold(
      appBar: AppBar(title: const Text('Payment Details')),
      body: paymentAsync.when(
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
                    ref.invalidate(paymentDetailsProvider(paymentId)),
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
        data: (payment) {
          final amount = (payment['amount'] as num?)?.toDouble() ?? 0;
          final paymentDate = payment['paymentDate'] != null
              ? DateTime.parse(payment['paymentDate'])
              : null;
          final paidFrom = payment['paidFrom'] != null
              ? DateTime.parse(payment['paidFrom'])
              : null;
          final paidUpto = payment['paidUpto'] != null
              ? DateTime.parse(payment['paidUpto'])
              : null;
          final modeCode = payment['paymentModeCode'] ?? '';
          final frequencyCode = payment['paymentFrequencyCode'] ?? '';
          final notes = payment['notes'] as String?;

          return SingleChildScrollView(
            padding: const EdgeInsets.all(AppSizes.paddingMd),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Amount header
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSizes.paddingLg),
                    child: Column(
                      children: [
                        CircleAvatar(
                          radius: 40,
                          backgroundColor:
                              AppColors.success.withValues(alpha: 0.15),
                          child: Icon(
                            _getModeIcon(modeCode),
                            size: 36,
                            color: AppColors.success,
                          ),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          currencyFormat.format(amount),
                          style:
                              Theme.of(context).textTheme.headlineMedium?.copyWith(
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.success,
                                  ),
                        ),
                        if (paymentDate != null) ...[
                          const SizedBox(height: 4),
                          Text(
                            dateFormat.format(paymentDate),
                            style: const TextStyle(
                                color: AppColors.textSecondary),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),

                // Payment info
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSizes.paddingMd),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Payment Information',
                          style: Theme.of(context)
                              .textTheme
                              .titleSmall
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                        const Divider(),
                        _InfoRow(
                          icon: _getModeIcon(modeCode),
                          label: 'Mode',
                          value: _getModeLabel(modeCode),
                        ),
                        _InfoRow(
                          icon: Icons.repeat,
                          label: 'Frequency',
                          value: frequencyCode,
                        ),
                        if (paidFrom != null)
                          _InfoRow(
                            icon: Icons.calendar_today,
                            label: 'Paid From',
                            value: dateFormat.format(paidFrom),
                          ),
                        if (paidUpto != null)
                          _InfoRow(
                            icon: Icons.calendar_today,
                            label: 'Paid Upto',
                            value: dateFormat.format(paidUpto),
                          ),
                      ],
                    ),
                  ),
                ),

                // Notes
                if (notes != null && notes.isNotEmpty) ...[
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
                          Text(notes),
                        ],
                      ),
                    ),
                  ),
                ],
                const SizedBox(height: 24),

                // Actions
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => _sendReceipt(context, ref),
                        icon: const Icon(Icons.email),
                        label: const Text('Email Receipt'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => _sendWhatsApp(context, ref),
                        icon: const Icon(Icons.chat),
                        label: const Text('WhatsApp'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  void _sendReceipt(BuildContext context, WidgetRef ref) {
    final emailController = TextEditingController();
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Send Receipt'),
        content: TextField(
          controller: emailController,
          decoration: const InputDecoration(
            labelText: 'Email Address',
            hintText: 'Enter email address',
          ),
          keyboardType: TextInputType.emailAddress,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () async {
              if (emailController.text.isNotEmpty) {
                Navigator.pop(ctx);
                try {
                  await ref
                      .read(paymentServiceProvider)
                      .sendReceipt(paymentId, emailController.text);
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Receipt sent!'),
                        backgroundColor: AppColors.success,
                      ),
                    );
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
              }
            },
            child: const Text('Send'),
          ),
        ],
      ),
    );
  }

  void _sendWhatsApp(BuildContext context, WidgetRef ref) async {
    try {
      await ref.read(paymentServiceProvider).sendReceiptWhatsapp(paymentId);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('WhatsApp receipt sent!'),
            backgroundColor: AppColors.success,
          ),
        );
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
  }

  IconData _getModeIcon(String mode) {
    switch (mode.toUpperCase()) {
      case 'UPI':
        return Icons.qr_code;
      case 'CASH':
        return Icons.money;
      case 'BANK':
        return Icons.account_balance;
      default:
        return Icons.payment;
    }
  }

  String _getModeLabel(String mode) {
    switch (mode.toUpperCase()) {
      case 'UPI':
        return 'UPI';
      case 'CASH':
        return 'Cash';
      case 'BANK':
        return 'Bank Transfer';
      default:
        return mode;
    }
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
                  color: AppColors.textSecondary, fontSize: 13),
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
