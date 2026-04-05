import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/constants/app_constants.dart';
import '../models/booking_models.dart';
import '../services/booking_service.dart';
import '../providers/booking_provider.dart';
import '../../rooms/providers/room_provider.dart';

class AddBookingScreen extends ConsumerStatefulWidget {
  const AddBookingScreen({super.key});

  @override
  ConsumerState<AddBookingScreen> createState() => _AddBookingScreenState();
}

class _AddBookingScreenState extends ConsumerState<AddBookingScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _aadharController = TextEditingController();
  final _contactController = TextEditingController();
  final _advanceController = TextEditingController();
  final _notesController = TextEditingController();

  String? _selectedRoomId;
  DateTime _scheduledCheckInDate = DateTime.now();
  String _paymentMode = 'CASH';
  bool _submitting = false;

  final _dateFormat = DateFormat('dd MMM yyyy');

  @override
  void dispose() {
    _nameController.dispose();
    _aadharController.dispose();
    _contactController.dispose();
    _advanceController.dispose();
    _notesController.dispose();
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

    final advanceAmount = double.tryParse(_advanceController.text);

    setState(() => _submitting = true);
    try {
      await ref.read(bookingServiceProvider).createBooking(
            CreateBookingRequest(
              aadharNumber: _aadharController.text.trim(),
              name: _nameController.text.trim(),
              contactNumber: _contactController.text.trim(),
              roomId: _selectedRoomId!,
              scheduledCheckInDate: _scheduledCheckInDate,
              advanceAmount: advanceAmount,
              paymentModeCode:
                  (advanceAmount != null && advanceAmount > 0) ? _paymentMode : null,
              notes: _notesController.text.isNotEmpty
                  ? _notesController.text
                  : null,
            ),
          );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Booking created successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
        ref.invalidate(bookingListProvider);
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
      appBar: AppBar(title: const Text('Add Booking')),
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
                  labelText: 'Name',
                  prefixIcon: Icon(Icons.person),
                ),
                textCapitalization: TextCapitalization.words,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Enter name';
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Aadhaar
              TextFormField(
                controller: _aadharController,
                decoration: const InputDecoration(
                  labelText: 'Aadhaar Number',
                  prefixIcon: Icon(Icons.credit_card),
                ),
                keyboardType: TextInputType.number,
                maxLength: 12,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Enter Aadhaar number';
                  if (!RegExp(r'^\d{12}$').hasMatch(v.trim())) {
                    return 'Aadhaar must be 12 digits';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Contact
              TextFormField(
                controller: _contactController,
                decoration: const InputDecoration(
                  labelText: 'Contact Number',
                  prefixIcon: Icon(Icons.phone),
                ),
                keyboardType: TextInputType.phone,
                maxLength: 10,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Enter contact number';
                  if (!RegExp(r'^[6-9]\d{9}$').hasMatch(v.trim())) {
                    return 'Enter a valid 10-digit mobile number';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Room
              Text('Room', style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              roomsAsync.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => Text('Error loading rooms: $e'),
                data: (rooms) {
                  final available =
                      rooms.where((r) => r.vacancies > 0).toList();
                  return DropdownButtonFormField<String>(
                    value: _selectedRoomId,
                    decoration: const InputDecoration(
                      prefixIcon: Icon(Icons.meeting_room),
                      hintText: 'Select room',
                    ),
                    items: available
                        .map((r) => DropdownMenuItem(
                              value: r.roomId,
                              child: Text(
                                  'Room ${r.roomNumber} (${r.vacancies} vacant)'),
                            ))
                        .toList(),
                    onChanged: (v) => setState(() => _selectedRoomId = v),
                    validator: (v) => v == null ? 'Select a room' : null,
                  );
                },
              ),
              const SizedBox(height: 16),

              // Scheduled Check-in Date
              Text('Scheduled Check-in Date',
                  style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: _scheduledCheckInDate,
                    firstDate: DateTime.now(),
                    lastDate: DateTime.now().add(const Duration(days: 365)),
                  );
                  if (picked != null) {
                    setState(() => _scheduledCheckInDate = picked);
                  }
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
                      Text(_dateFormat.format(_scheduledCheckInDate),
                          style: const TextStyle(fontSize: 16)),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Advance Amount (optional)
              TextFormField(
                controller: _advanceController,
                decoration: const InputDecoration(
                  labelText: 'Advance Amount (optional)',
                  prefixIcon: Icon(Icons.currency_rupee),
                ),
                keyboardType: TextInputType.number,
                validator: (v) {
                  if (v != null && v.isNotEmpty && double.tryParse(v) == null) {
                    return 'Invalid amount';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Payment Mode (shown if advance > 0)
              Text('Payment Mode (for advance)',
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
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.check),
                  label: Text(
                      _submitting ? 'Creating...' : 'Create Booking'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
