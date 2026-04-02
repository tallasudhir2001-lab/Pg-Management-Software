import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../models/pending_rent_models.dart';
import '../services/payment_service.dart';
import '../providers/payment_provider.dart';
import '../../tenants/models/tenant_models.dart';
import '../../tenants/services/tenant_service.dart';

class AddPaymentScreen extends ConsumerStatefulWidget {
  final String? preselectedTenantId;
  final String? preselectedTenantName;

  const AddPaymentScreen({
    super.key,
    this.preselectedTenantId,
    this.preselectedTenantName,
  });

  @override
  ConsumerState<AddPaymentScreen> createState() => _AddPaymentScreenState();
}

class _AddPaymentScreenState extends ConsumerState<AddPaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();

  String? _selectedTenantId;
  String? _selectedTenantName;
  String _paymentMode = 'UPI';
  String _frequency = 'MONTHLY';
  DateTime _paymentDate = DateTime.now();
  DateTime? _paidUpto;
  double? _amount;

  PendingRentResponse? _pendingRent;
  bool _loadingPending = false;
  bool _submitting = false;

  final _currencyFormat = NumberFormat.currency(locale: 'en_IN', symbol: '₹');
  final _dateFormat = DateFormat('dd MMM yyyy');

  @override
  void initState() {
    super.initState();
    if (widget.preselectedTenantId != null) {
      _selectedTenantId = widget.preselectedTenantId;
      _selectedTenantName = widget.preselectedTenantName;
      _loadPendingRent(widget.preselectedTenantId!);
    }
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _loadPendingRent(String tenantId) async {
    setState(() => _loadingPending = true);
    try {
      final pending = await ref.read(paymentServiceProvider).getPendingRent(tenantId);
      setState(() {
        _pendingRent = pending;
        _loadingPending = false;
        if (pending.breakdown.isNotEmpty) {
          _amount = pending.breakdown.first.amount;
          _paidUpto = pending.breakdown.first.toDate;
        }
      });
    } catch (e) {
      setState(() => _loadingPending = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error loading rent info: $e')),
        );
      }
    }
  }

  Future<void> _selectTenant() async {
    final result = await showModalBottomSheet<TenantListItem>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => const _TenantPickerSheet(),
    );
    if (result != null) {
      setState(() {
        _selectedTenantId = result.tenantId;
        _selectedTenantName = result.name;
        _pendingRent = null;
        _amount = null;
        _paidUpto = null;
      });
      _loadPendingRent(result.tenantId);
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedTenantId == null || _paidUpto == null || _amount == null) return;

    setState(() => _submitting = true);
    try {
      await ref.read(paymentServiceProvider).createPayment(
            CreatePaymentRequest(
              tenantId: _selectedTenantId!,
              amount: _amount!,
              paymentDate: _paymentDate,
              paidUpto: _paidUpto!,
              paymentFrequencyCode: _frequency,
              paymentModeCode: _paymentMode,
              notes: _notesController.text.isNotEmpty ? _notesController.text : null,
            ),
          );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Payment recorded successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
        // Refresh payment list
        ref.invalidate(paymentListProvider);
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      setState(() => _submitting = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: AppColors.error),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Add Payment')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSizes.paddingMd),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Tenant selector
              Text('Tenant', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              InkWell(
                onTap: _selectTenant,
                child: Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    border: Border.all(color: AppColors.divider),
                    borderRadius: BorderRadius.circular(AppSizes.borderRadiusSm),
                    color: Colors.white,
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.person, color: AppColors.textSecondary),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          _selectedTenantName ?? 'Select Tenant',
                          style: TextStyle(
                            color: _selectedTenantName != null
                                ? AppColors.textPrimary
                                : AppColors.textSecondary,
                            fontSize: 16,
                          ),
                        ),
                      ),
                      const Icon(Icons.arrow_drop_down),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Pending rent info
              if (_loadingPending)
                const Center(child: Padding(
                  padding: EdgeInsets.all(16),
                  child: CircularProgressIndicator(),
                )),

              if (_pendingRent != null && !_loadingPending) ...[
                Card(
                  color: AppColors.warning.withValues(alpha: 0.08),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Text(
                              'Pending Rent',
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            Text(
                              _currencyFormat.format(_pendingRent!.totalPendingAmount),
                              style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                color: AppColors.error,
                                fontSize: 18,
                              ),
                            ),
                          ],
                        ),
                        if (_pendingRent!.breakdown.isNotEmpty) ...[
                          const Divider(),
                          ..._pendingRent!.breakdown.map((b) => Padding(
                                padding: const EdgeInsets.symmetric(vertical: 2),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Text(
                                      'Room ${b.roomNumber}: ${_dateFormat.format(b.fromDate)} - ${_dateFormat.format(b.toDate)}',
                                      style: const TextStyle(fontSize: 12),
                                    ),
                                    Text(
                                      _currencyFormat.format(b.amount),
                                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
                                    ),
                                  ],
                                ),
                              )),
                        ],
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),

                // Paid Upto date
                Text('Paid Upto', style: Theme.of(context).textTheme.labelLarge),
                const SizedBox(height: 8),
                InkWell(
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: context,
                      initialDate: _paidUpto ?? DateTime.now(),
                      firstDate: DateTime(2020),
                      lastDate: DateTime(2030),
                    );
                    if (picked != null) setState(() => _paidUpto = picked);
                  },
                  child: Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      border: Border.all(color: AppColors.divider),
                      borderRadius: BorderRadius.circular(AppSizes.borderRadiusSm),
                      color: Colors.white,
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.calendar_today, size: 20, color: AppColors.textSecondary),
                        const SizedBox(width: 12),
                        Text(
                          _paidUpto != null ? _dateFormat.format(_paidUpto!) : 'Select date',
                          style: const TextStyle(fontSize: 16),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),

                // Amount
                TextFormField(
                  initialValue: _amount?.toStringAsFixed(0),
                  decoration: const InputDecoration(
                    labelText: 'Amount',
                    prefixIcon: Icon(Icons.currency_rupee),
                  ),
                  keyboardType: TextInputType.number,
                  onChanged: (v) => _amount = double.tryParse(v),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Enter amount';
                    if (double.tryParse(v) == null) return 'Invalid amount';
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Payment Mode
                Text('Payment Mode', style: Theme.of(context).textTheme.labelLarge),
                const SizedBox(height: 8),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'UPI', label: Text('UPI'), icon: Icon(Icons.qr_code, size: 18)),
                    ButtonSegment(value: 'CASH', label: Text('Cash'), icon: Icon(Icons.money, size: 18)),
                    ButtonSegment(value: 'BANK', label: Text('Bank'), icon: Icon(Icons.account_balance, size: 18)),
                  ],
                  selected: {_paymentMode},
                  onSelectionChanged: (v) => setState(() => _paymentMode = v.first),
                ),
                const SizedBox(height: 16),

                // Payment Date
                Text('Payment Date', style: Theme.of(context).textTheme.labelLarge),
                const SizedBox(height: 8),
                InkWell(
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: context,
                      initialDate: _paymentDate,
                      firstDate: DateTime(2020),
                      lastDate: DateTime.now(),
                    );
                    if (picked != null) setState(() => _paymentDate = picked);
                  },
                  child: Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      border: Border.all(color: AppColors.divider),
                      borderRadius: BorderRadius.circular(AppSizes.borderRadiusSm),
                      color: Colors.white,
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.calendar_today, size: 20, color: AppColors.textSecondary),
                        const SizedBox(width: 12),
                        Text(_dateFormat.format(_paymentDate), style: const TextStyle(fontSize: 16)),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),

                // Notes
                TextFormField(
                  controller: _notesController,
                  decoration: const InputDecoration(
                    labelText: 'Notes (optional)',
                    prefixIcon: Icon(Icons.notes),
                  ),
                  maxLines: 2,
                  maxLength: 250,
                ),
                const SizedBox(height: 24),

                // Submit
                SizedBox(
                  height: 52,
                  child: ElevatedButton.icon(
                    onPressed: _submitting ? null : _submit,
                    icon: _submitting
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                          )
                        : const Icon(Icons.check),
                    label: Text(_submitting ? 'Recording...' : 'Record Payment'),
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

/// Bottom sheet to pick a tenant
class _TenantPickerSheet extends ConsumerStatefulWidget {
  const _TenantPickerSheet();

  @override
  ConsumerState<_TenantPickerSheet> createState() => _TenantPickerSheetState();
}

class _TenantPickerSheetState extends ConsumerState<_TenantPickerSheet> {
  List<TenantListItem> _tenants = [];
  List<TenantListItem> _filtered = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadTenants();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadTenants() async {
    try {
      final result = await ref.read(tenantServiceProvider).getTenants(
            page: 1,
            pageSize: 100,
            status: 'ACTIVE',
          );
      setState(() {
        _tenants = result.items;
        _filtered = result.items;
        _loading = false;
      });
    } catch (e) {
      setState(() => _loading = false);
    }
  }

  void _filter(String query) {
    setState(() {
      _filtered = _tenants
          .where((t) => t.name.toLowerCase().contains(query.toLowerCase()))
          .toList();
    });
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.7,
      maxChildSize: 0.9,
      minChildSize: 0.4,
      expand: false,
      builder: (context, scrollController) {
        return Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: TextField(
                controller: _searchController,
                onChanged: _filter,
                decoration: InputDecoration(
                  hintText: 'Search tenants...',
                  prefixIcon: const Icon(Icons.search),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(AppSizes.borderRadiusSm),
                  ),
                ),
              ),
            ),
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : ListView.builder(
                      controller: scrollController,
                      itemCount: _filtered.length,
                      itemBuilder: (context, index) {
                        final tenant = _filtered[index];
                        return ListTile(
                          leading: CircleAvatar(
                            child: Text(tenant.name[0].toUpperCase()),
                          ),
                          title: Text(tenant.name),
                          subtitle: Text(
                            tenant.roomNumber != null
                                ? 'Room ${tenant.roomNumber}'
                                : 'No room',
                          ),
                          onTap: () => Navigator.pop(context, tenant),
                        );
                      },
                    ),
            ),
          ],
        );
      },
    );
  }
}
