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
