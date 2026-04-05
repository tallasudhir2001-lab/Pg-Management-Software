import 'package:flutter/material.dart';
import '../../../core/constants/app_constants.dart';
import '../../../core/router/app_routes.dart';

class MoreScreen extends StatelessWidget {
  const MoreScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('More')),
      body: ListView(
        padding: const EdgeInsets.all(AppSizes.paddingMd),
        children: [
          _MoreItem(
            icon: Icons.event_note,
            color: Colors.indigo,
            title: 'Bookings',
            subtitle: 'Manage room bookings',
            onTap: () => Navigator.pushNamed(context, AppRoutes.bookings),
          ),
          _MoreItem(
            icon: Icons.receipt_long,
            color: AppColors.error,
            title: 'Expenses',
            subtitle: 'Track and record expenses',
            onTap: () => Navigator.pushNamed(context, AppRoutes.expenses),
          ),
        ],
      ),
    );
  }
}

class _MoreItem extends StatelessWidget {
  final IconData icon;
  final Color color;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _MoreItem({
    required this.icon,
    required this.color,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: color.withValues(alpha: 0.12),
          child: Icon(icon, color: color),
        ),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(subtitle),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
