class BookingListItem {
  final String bookingId;
  final String tenantId;
  final String tenantName;
  final String? tenantContact;
  final String roomId;
  final String roomNumber;
  final DateTime scheduledCheckInDate;
  final String status;
  final double advanceAmount;
  final String? notes;
  final DateTime createdAt;

  BookingListItem({
    required this.bookingId,
    required this.tenantId,
    required this.tenantName,
    this.tenantContact,
    required this.roomId,
    required this.roomNumber,
    required this.scheduledCheckInDate,
    required this.status,
    required this.advanceAmount,
    this.notes,
    required this.createdAt,
  });

  factory BookingListItem.fromJson(Map<String, dynamic> json) {
    return BookingListItem(
      bookingId: json['bookingId'],
      tenantId: json['tenantId'],
      tenantName: json['tenantName'] ?? '',
      tenantContact: json['tenantContact'],
      roomId: json['roomId'],
      roomNumber: json['roomNumber'] ?? '',
      scheduledCheckInDate: DateTime.parse(json['scheduledCheckInDate']),
      status: json['status'] ?? 'Active',
      advanceAmount: (json['advanceAmount'] ?? 0).toDouble(),
      notes: json['notes'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}

class BookingDetails {
  final String bookingId;
  final String tenantId;
  final String tenantName;
  final String tenantContact;
  final String roomId;
  final String roomNumber;
  final DateTime scheduledCheckInDate;
  final String status;
  final double advanceAmount;
  final String? notes;
  final DateTime createdAt;
  final String createdBy;

  BookingDetails({
    required this.bookingId,
    required this.tenantId,
    required this.tenantName,
    required this.tenantContact,
    required this.roomId,
    required this.roomNumber,
    required this.scheduledCheckInDate,
    required this.status,
    required this.advanceAmount,
    this.notes,
    required this.createdAt,
    required this.createdBy,
  });

  factory BookingDetails.fromJson(Map<String, dynamic> json) {
    return BookingDetails(
      bookingId: json['bookingId'],
      tenantId: json['tenantId'],
      tenantName: json['tenantName'] ?? '',
      tenantContact: json['tenantContact'] ?? '',
      roomId: json['roomId'],
      roomNumber: json['roomNumber'] ?? '',
      scheduledCheckInDate: DateTime.parse(json['scheduledCheckInDate']),
      status: json['status'] ?? 'Active',
      advanceAmount: (json['advanceAmount'] ?? 0).toDouble(),
      notes: json['notes'],
      createdAt: DateTime.parse(json['createdAt']),
      createdBy: json['createdBy'] ?? '',
    );
  }
}

class CreateBookingRequest {
  final String aadharNumber;
  final String name;
  final String contactNumber;
  final String roomId;
  final DateTime scheduledCheckInDate;
  final double? advanceAmount;
  final String? paymentModeCode;
  final String? notes;

  CreateBookingRequest({
    required this.aadharNumber,
    required this.name,
    required this.contactNumber,
    required this.roomId,
    required this.scheduledCheckInDate,
    this.advanceAmount,
    this.paymentModeCode,
    this.notes,
  });

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{
      'aadharNumber': aadharNumber,
      'name': name,
      'contactNumber': contactNumber,
      'roomId': roomId,
      'scheduledCheckInDate': scheduledCheckInDate.toIso8601String(),
    };
    if (advanceAmount != null && advanceAmount! > 0) {
      map['advanceAmount'] = advanceAmount;
      if (paymentModeCode != null) map['paymentModeCode'] = paymentModeCode;
    }
    if (notes != null && notes!.isNotEmpty) map['notes'] = notes;
    return map;
  }
}
