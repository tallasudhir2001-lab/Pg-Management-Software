class TenantListItem {
  final String tenantId;
  final String name;
  final String? roomId;
  final String? roomNumber;
  final String? contactNumber;
  final String status;
  final DateTime? checkedInAt;
  final bool isRentPending;
  final DateTime? lastPaymentDate;
  final DateTime? overdueSince;
  final int? daysOverdue;
  final String? stayType;

  TenantListItem({
    required this.tenantId,
    required this.name,
    this.roomId,
    this.roomNumber,
    this.contactNumber,
    required this.status,
    this.checkedInAt,
    this.isRentPending = false,
    this.lastPaymentDate,
    this.overdueSince,
    this.daysOverdue,
    this.stayType,
  });

  factory TenantListItem.fromJson(Map<String, dynamic> json) {
    return TenantListItem(
      tenantId: json['tenantId'],
      name: json['name'] ?? '',
      roomId: json['roomId'],
      roomNumber: json['roomNumber'],
      contactNumber: json['contactNumber'],
      status: json['status'] ?? 'ACTIVE',
      checkedInAt: json['checkedInAt'] != null
          ? DateTime.parse(json['checkedInAt'])
          : null,
      isRentPending: json['isRentPending'] ?? false,
      lastPaymentDate: json['lastPaymentDate'] != null
          ? DateTime.parse(json['lastPaymentDate'])
          : null,
      overdueSince: json['overdueSince'] != null
          ? DateTime.parse(json['overdueSince'])
          : null,
      daysOverdue: json['daysOverdue'],
      stayType: json['stayType'],
    );
  }
}

class TenantDetails {
  final String tenantId;
  final String name;
  final String? contactNumber;
  final String? aadharNumber;
  final String? email;
  final String? notes;
  final Map<String, dynamic>? activeStay;
  final List<dynamic>? payments;
  final List<dynamic>? advances;
  final Map<String, dynamic>? booking;

  TenantDetails({
    required this.tenantId,
    required this.name,
    this.contactNumber,
    this.aadharNumber,
    this.email,
    this.notes,
    this.activeStay,
    this.payments,
    this.advances,
    this.booking,
  });

  factory TenantDetails.fromJson(Map<String, dynamic> json) {
    return TenantDetails(
      tenantId: json['tenantId'],
      name: json['name'] ?? '',
      contactNumber: json['contactNumber'],
      aadharNumber: json['aadharNumber'],
      email: json['email'],
      notes: json['notes'],
      activeStay: json['activeStay'],
      payments: json['payments'],
      advances: json['advances'],
      booking: json['booking'],
    );
  }

  String? get roomNumber => activeStay?['roomNumber'];
  String? get stayType => activeStay?['stayType'];
  DateTime? get fromDate =>
      activeStay?['fromDate'] != null ? DateTime.parse(activeStay!['fromDate']) : null;
  DateTime? get toDate =>
      activeStay?['toDate'] != null ? DateTime.parse(activeStay!['toDate']) : null;
  double? get rentAmount => (activeStay?['rentAmount'] as num?)?.toDouble();
}
