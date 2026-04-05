import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../models/expense_models.dart';
import '../services/expense_service.dart';
import '../providers/expense_provider.dart';

class AddExpenseScreen extends ConsumerStatefulWidget {
  const AddExpenseScreen({super.key});

  @override
  ConsumerState<AddExpenseScreen> createState() => _AddExpenseScreenState();
}

class _AddExpenseScreenState extends ConsumerState<AddExpenseScreen> {
  final _formKey = GlobalKey<FormState>();
  final _descriptionController = TextEditingController();
  final _amountController = TextEditingController();
  final _referenceController = TextEditingController();

  int? _selectedCategoryId;
  String _paymentMode = 'CASH';
  DateTime _expenseDate = DateTime.now();
  bool _submitting = false;

  final _dateFormat = DateFormat('dd MMM yyyy');

  @override
  void dispose() {
    _descriptionController.dispose();
    _amountController.dispose();
    _referenceController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedCategoryId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select a category')),
      );
      return;
    }

    setState(() => _submitting = true);
    try {
      await ref.read(expenseServiceProvider).createExpense(
            CreateExpenseRequest(
              categoryId: _selectedCategoryId!,
              amount: double.parse(_amountController.text),
              expenseDate: _expenseDate,
              description: _descriptionController.text,
              paymentModeCode: _paymentMode,
              referenceNo: _referenceController.text.isNotEmpty
                  ? _referenceController.text
                  : null,
            ),
          );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Expense recorded successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
        ref.invalidate(expenseListProvider);
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
    final categoriesAsync = ref.watch(expenseCategoriesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Add Expense')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSizes.paddingMd),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Category
              Text('Category', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              categoriesAsync.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => Text('Error loading categories: $e'),
                data: (categories) => DropdownButtonFormField<int>(
                  value: _selectedCategoryId,
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.category),
                    hintText: 'Select category',
                  ),
                  items: categories
                      .map((c) => DropdownMenuItem(
                            value: c.id,
                            child: Text(c.name),
                          ))
                      .toList(),
                  onChanged: (v) => setState(() => _selectedCategoryId = v),
                  validator: (v) => v == null ? 'Select a category' : null,
                ),
              ),
              const SizedBox(height: 16),

              // Amount
              TextFormField(
                controller: _amountController,
                decoration: const InputDecoration(
                  labelText: 'Amount',
                  prefixIcon: Icon(Icons.currency_rupee),
                ),
                keyboardType: TextInputType.number,
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Enter amount';
                  if (double.tryParse(v) == null) return 'Invalid amount';
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Expense Date
              Text('Expense Date', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: _expenseDate,
                    firstDate: DateTime(2020),
                    lastDate: DateTime.now(),
                  );
                  if (picked != null) setState(() => _expenseDate = picked);
                },
                child: Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    border: Border.all(color: AppColors.divider),
                    borderRadius:
                        BorderRadius.circular(AppSizes.borderRadiusSm),
                    color: Colors.white,
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.calendar_today,
                          size: 20, color: AppColors.textSecondary),
                      const SizedBox(width: 12),
                      Text(_dateFormat.format(_expenseDate),
                          style: const TextStyle(fontSize: 16)),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Payment Mode
              Text('Payment Mode',
                  style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(
                      value: 'CASH',
                      label: Text('Cash'),
                      icon: Icon(Icons.money, size: 18)),
                  ButtonSegment(
                      value: 'UPI',
                      label: Text('UPI'),
                      icon: Icon(Icons.qr_code, size: 18)),
                  ButtonSegment(
                      value: 'BANK',
                      label: Text('Bank'),
                      icon: Icon(Icons.account_balance, size: 18)),
                ],
                selected: {_paymentMode},
                onSelectionChanged: (v) =>
                    setState(() => _paymentMode = v.first),
              ),
              const SizedBox(height: 16),

              // Description
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(
                  labelText: 'Description',
                  prefixIcon: Icon(Icons.description),
                ),
                maxLines: 2,
                maxLength: 250,
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Enter description';
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Reference No (optional)
              TextFormField(
                controller: _referenceController,
                decoration: const InputDecoration(
                  labelText: 'Reference No (optional)',
                  prefixIcon: Icon(Icons.tag),
                ),
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
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.check),
                  label:
                      Text(_submitting ? 'Recording...' : 'Record Expense'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
