import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../models/create_tenant_models.dart';
import '../services/tenant_service.dart';
import '../providers/tenant_provider.dart';
import '../../rooms/providers/room_provider.dart';
import '../../payments/services/payment_service.dart';

class AddTenantScreen extends ConsumerStatefulWidget {
  const AddTenantScreen({super.key});

  @override
  ConsumerState<AddTenantScreen> createState() => _AddTenantScreenState();
}

class _AddTenantScreenState extends ConsumerState<AddTenantScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _contactController = TextEditingController();
  final _aadharController = TextEditingController();
  final _emailController = TextEditingController();
  final _notesController = TextEditingController();
  final _advanceAmountController = TextEditingController();
  final _initialPaymentAmountController = TextEditingController();

  String? _selectedRoomId;
  DateTime _fromDate = DateTime.now();
  String _stayType = 'MONTHLY';
  bool _hasAdvance = false;
  String _advancePaymentMode = 'UPI';
  bool _hasPayment = false;
  String _initialPaymentMode = 'UPI';
  DateTime _paidFrom = DateTime.now();
  DateTime _paidUpto = DateTime.now().add(const Duration(days: 30));
  bool _submitting = false;

  final _dateFormat = DateFormat('dd MMM yyyy');

  @override
  void dispose() {
    _nameController.dispose();
    _contactController.dispose();
    _aadharController.dispose();
    _emailController.dispose();
    _notesController.dispose();
    _advanceAmountController.dispose();
    _initialPaymentAmountController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedRoomId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select a room')),
      );
      return;
    }
    if (_hasPayment) {
      final amt = double.tryParse(_initialPaymentAmountController.text);
      if (amt == null || amt <= 0) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Enter a valid payment amount')),
        );
        return;
      }
    }

    setState(() => _submitting = true);
    try {
      final tenantId = await ref.read(tenantServiceProvider).createTenant(
            CreateTenantRequest(
              name: _nameController.text.trim(),
              contactNumber: _contactController.text.trim(),
              aadharNumber: _aadharController.text.trim(),
              email: _emailController.text.trim(),
              roomId: _selectedRoomId,
              fromDate: _fromDate,
              stayType: _stayType,
              hasAdvance: _hasAdvance,
              advanceAmount: _hasAdvance
                  ? double.tryParse(_advanceAmountController.text)
                  : null,
              paymentModeCode: _hasAdvance ? _advancePaymentMode : null,
              notes: _notesController.text,
            ),
          );

      // Create initial payment if requested
      if (_hasPayment && tenantId.isNotEmpty && mounted) {
        try {
          await ref.read(paymentServiceProvider).createPayment({
            'tenantId': tenantId,
            'amount': double.parse(_initialPaymentAmountController.text),
            'paymentModeCode': _initialPaymentMode,
            'paymentFrequencyCode': _stayType,
            'paidUpto': _paidUpto.toIso8601String(),
          });
        } catch (_) {
          // Tenant created but payment failed
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Tenant created. Initial payment failed — add it manually.'),
                backgroundColor: AppColors.warning,
              ),
            );
            ref.invalidate(tenantListProvider);
            ref.invalidate(roomListProvider);
            Navigator.of(context).pop(true);
            return;
          }
        }
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(_hasPayment
                ? 'Tenant and initial payment created!'
                : 'Tenant added successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
        ref.invalidate(tenantListProvider);
        ref.invalidate(roomListProvider);
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
    final roomsAsync = ref.watch(allRoomsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Add Tenant')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSizes.paddingMd),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Name
              TextFormField(
                controller: _nameController,
                decoration: const InputDecoration(
                  labelText: 'Full Name *',
                  prefixIcon: Icon(Icons.person),
                ),
                textCapitalization: TextCapitalization.words,
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Name is required' : null,
              ),
              const SizedBox(height: 14),

              // Contact
              TextFormField(
                controller: _contactController,
                decoration: const InputDecoration(
                  labelText: 'Contact Number *',
                  prefixIcon: Icon(Icons.phone),
                ),
                keyboardType: TextInputType.phone,
                maxLength: 10,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Contact is required';
                  if (v.trim().length != 10) return 'Enter 10 digit number';
                  return null;
                },
              ),
              const SizedBox(height: 14),

              // Email
              TextFormField(
                controller: _emailController,
                decoration: const InputDecoration(
                  labelText: 'Email *',
                  prefixIcon: Icon(Icons.email),
                ),
                keyboardType: TextInputType.emailAddress,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Email is required';
                  if (!v.contains('@')) return 'Enter a valid email';
                  return null;
                },
              ),
              const SizedBox(height: 14),

              // Aadhaar
              TextFormField(
                controller: _aadharController,
                decoration: const InputDecoration(
                  labelText: 'Aadhaar Number *',
                  prefixIcon: Icon(Icons.credit_card),
                ),
                keyboardType: TextInputType.number,
                maxLength: 12,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Aadhaar is required';
                  if (v.trim().length != 12) return 'Enter 12 digit number';
                  return null;
                },
              ),
              const SizedBox(height: 14),

              // Room selector
              Text('Room *', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              roomsAsync.when(
                data: (rooms) {
                  final available = rooms.where((r) => r.vacancies > 0).toList();
                  return DropdownButtonFormField<String>(
                    value: _selectedRoomId,
                    decoration: const InputDecoration(
                      prefixIcon: Icon(Icons.meeting_room),
                    ),
                    hint: const Text('Select Room'),
                    items: available.map((r) {
                      return DropdownMenuItem(
                        value: r.roomId,
                        child: Text(
                          'Room ${r.roomNumber} (${r.vacancies} vacant) - ₹${r.rentAmount.toStringAsFixed(0)}',
                        ),
                      );
                    }).toList(),
                    onChanged: (v) => setState(() => _selectedRoomId = v),
                  );
                },
                loading: () => const LinearProgressIndicator(),
                error: (e, _) => Text('Error loading rooms: $e'),
              ),
              const SizedBox(height: 14),

              // Check-in date
              Text('Check-in Date', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: _fromDate,
                    firstDate: DateTime(2020),
                    lastDate: DateTime(2030),
                  );
                  if (picked != null) setState(() => _fromDate = picked);
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
                      Text(_dateFormat.format(_fromDate), style: const TextStyle(fontSize: 16)),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 14),

              // Stay type
              Text('Stay Type', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'MONTHLY', label: Text('Monthly')),
                  ButtonSegment(value: 'DAILY', label: Text('Daily')),
                ],
                selected: {_stayType},
                onSelectionChanged: (v) => setState(() => _stayType = v.first),
              ),
              const SizedBox(height: 14),

              // Advance toggle
              SwitchListTile(
                title: const Text('Collect Advance'),
                value: _hasAdvance,
                onChanged: (v) => setState(() => _hasAdvance = v),
                contentPadding: EdgeInsets.zero,
              ),

              if (_hasAdvance) ...[
                TextFormField(
                  controller: _advanceAmountController,
                  decoration: const InputDecoration(
                    labelText: 'Advance Amount',
                    prefixIcon: Icon(Icons.currency_rupee),
                  ),
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (_hasAdvance && (v == null || v.isEmpty)) return 'Enter advance amount';
                    return null;
                  },
                ),
                const SizedBox(height: 8),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'UPI', label: Text('UPI')),
                    ButtonSegment(value: 'CASH', label: Text('Cash')),
                    ButtonSegment(value: 'BANK', label: Text('Bank')),
                  ],
                  selected: {_advancePaymentMode},
                  onSelectionChanged: (v) => setState(() => _advancePaymentMode = v.first),
                ),
              ],
              const SizedBox(height: 14),

              // Initial payment toggle
              SwitchListTile(
                title: const Text('Collect Initial Rent Payment'),
                value: _hasPayment,
                onChanged: (v) => setState(() => _hasPayment = v),
                contentPadding: EdgeInsets.zero,
              ),

              if (_hasPayment) ...[
                TextFormField(
                  controller: _initialPaymentAmountController,
                  decoration: const InputDecoration(
                    labelText: 'Payment Amount',
                    prefixIcon: Icon(Icons.currency_rupee),
                  ),
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (_hasPayment && (v == null || v.isEmpty)) return 'Enter payment amount';
                    return null;
                  },
                ),
                const SizedBox(height: 8),
                Text('Payment Mode', style: Theme.of(context).textTheme.labelLarge),
                const SizedBox(height: 8),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'UPI', label: Text('UPI')),
                    ButtonSegment(value: 'CASH', label: Text('Cash')),
                    ButtonSegment(value: 'BANK', label: Text('Bank')),
                  ],
                  selected: {_initialPaymentMode},
                  onSelectionChanged: (v) => setState(() => _initialPaymentMode = v.first),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Paid From', style: Theme.of(context).textTheme.labelMedium),
                          const SizedBox(height: 4),
                          InkWell(
                            onTap: () async {
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: _paidFrom,
                                firstDate: DateTime(2020),
                                lastDate: DateTime(2030),
                              );
                              if (picked != null) setState(() => _paidFrom = picked);
                            },
                            child: Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                border: Border.all(color: AppColors.divider),
                                borderRadius: BorderRadius.circular(AppSizes.borderRadiusSm),
                              ),
                              child: Row(
                                children: [
                                  const Icon(Icons.calendar_today, size: 16, color: AppColors.textSecondary),
                                  const SizedBox(width: 8),
                                  Text(_dateFormat.format(_paidFrom), style: const TextStyle(fontSize: 14)),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Paid Upto', style: Theme.of(context).textTheme.labelMedium),
                          const SizedBox(height: 4),
                          InkWell(
                            onTap: () async {
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: _paidUpto,
                                firstDate: DateTime(2020),
                                lastDate: DateTime(2030),
                              );
                              if (picked != null) setState(() => _paidUpto = picked);
                            },
                            child: Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                border: Border.all(color: AppColors.divider),
                                borderRadius: BorderRadius.circular(AppSizes.borderRadiusSm),
                              ),
                              child: Row(
                                children: [
                                  const Icon(Icons.calendar_today, size: 16, color: AppColors.textSecondary),
                                  const SizedBox(width: 8),
                                  Text(_dateFormat.format(_paidUpto), style: const TextStyle(fontSize: 14)),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
              const SizedBox(height: 14),

              // Notes
              TextFormField(
                controller: _notesController,
                decoration: const InputDecoration(
                  labelText: 'Notes (optional)',
                  prefixIcon: Icon(Icons.notes),
                ),
                maxLines: 2,
              ),
              const SizedBox(height: 24),

              // Submit
              SizedBox(
                height: 52,
                child: ElevatedButton.icon(
                  onPressed: _submitting ? null : _submit,
                  icon: _submitting
                      ? const SizedBox(
                          width: 20, height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.person_add),
                  label: Text(_submitting ? 'Adding...' : 'Add Tenant'),
                ),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}
