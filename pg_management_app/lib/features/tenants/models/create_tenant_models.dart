class CreateTenantRequest {
  final String name;
  final DateTime? fromDate;
  final DateTime? toDate;
  final String? roomId;
  final String contactNumber;
  final String aadharNumber;
  final bool hasAdvance;
  final double? advanceAmount;
  final String? paymentModeCode;
  final String notes;
  final String email;
  final String stayType;

  CreateTenantRequest({
    required this.name,
    this.fromDate,
    this.toDate,
    this.roomId,
    required this.contactNumber,
    required this.aadharNumber,
    this.hasAdvance = false,
    this.advanceAmount,
    this.paymentModeCode,
    this.notes = '',
    required this.email,
    this.stayType = 'MONTHLY',
  });

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{
      'name': name,
      'contactNumber': contactNumber,
      'aadharNumber': aadharNumber,
      'hasAdvance': hasAdvance,
      'notes': notes,
      'email': email,
      'stayType': stayType,
    };
    if (fromDate != null) map['fromDate'] = fromDate!.toIso8601String();
    if (toDate != null) map['toDate'] = toDate!.toIso8601String();
    if (roomId != null) map['roomId'] = roomId;
    if (hasAdvance && advanceAmount != null) {
      map['advanceAmount'] = advanceAmount;
      if (paymentModeCode != null) map['paymentModeCode'] = paymentModeCode;
    }
    return map;
  }
}
