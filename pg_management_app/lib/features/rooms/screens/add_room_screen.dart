import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/app_constants.dart';
import '../services/room_service.dart';
import '../providers/room_provider.dart';

class AddRoomScreen extends ConsumerStatefulWidget {
  const AddRoomScreen({super.key});

  @override
  ConsumerState<AddRoomScreen> createState() => _AddRoomScreenState();
}

class _AddRoomScreenState extends ConsumerState<AddRoomScreen> {
  final _formKey = GlobalKey<FormState>();
  final _roomNumberController = TextEditingController();
  final _capacityController = TextEditingController(text: '1');
  final _rentAmountController = TextEditingController();

  bool _isAc = false;
  bool _submitting = false;

  @override
  void dispose() {
    _roomNumberController.dispose();
    _capacityController.dispose();
    _rentAmountController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _submitting = true);
    try {
      await ref.read(roomServiceProvider).createRoom({
        'roomNumber': _roomNumberController.text.trim(),
        'capacity': int.parse(_capacityController.text),
        'rentAmount': double.parse(_rentAmountController.text),
        'isAc': _isAc,
      });

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Room added successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
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
    return Scaffold(
      appBar: AppBar(title: const Text('Add Room')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSizes.paddingMd),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Room Number
              TextFormField(
                controller: _roomNumberController,
                decoration: const InputDecoration(
                  labelText: 'Room Number',
                  prefixIcon: Icon(Icons.meeting_room),
                ),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Enter room number';
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Capacity
              TextFormField(
                controller: _capacityController,
                decoration: const InputDecoration(
                  labelText: 'Capacity (beds)',
                  prefixIcon: Icon(Icons.people),
                ),
                keyboardType: TextInputType.number,
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Enter capacity';
                  final n = int.tryParse(v);
                  if (n == null || n < 1 || n > 20) {
                    return 'Capacity must be 1-20';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Rent Amount
              TextFormField(
                controller: _rentAmountController,
                decoration: const InputDecoration(
                  labelText: 'Rent Amount',
                  prefixIcon: Icon(Icons.currency_rupee),
                ),
                keyboardType: TextInputType.number,
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Enter rent amount';
                  if (double.tryParse(v) == null) return 'Invalid amount';
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // AC toggle
              SwitchListTile(
                title: const Text('Air Conditioned'),
                subtitle: const Text('Does this room have AC?'),
                secondary: Icon(
                  Icons.ac_unit,
                  color: _isAc ? AppColors.primary : AppColors.textSecondary,
                ),
                value: _isAc,
                onChanged: (v) => setState(() => _isAc = v),
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
                  label: Text(_submitting ? 'Adding...' : 'Add Room'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
