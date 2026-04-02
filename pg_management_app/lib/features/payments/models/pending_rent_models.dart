class PendingRentResponse {
  final String tenantId;
  final DateTime asOfDate;
  final double totalPendingAmount;
  final List<PendingRentBreakdown> breakdown;

  PendingRentResponse({
    required this.tenantId,
    required this.asOfDate,
    required this.totalPendingAmount,
    required this.breakdown,
  });

  factory PendingRentResponse.fromJson(Map<String, dynamic> json) {
    return PendingRentResponse(
      tenantId: json['tenantId'],
      asOfDate: DateTime.parse(json['asOfDate']),
      totalPendingAmount: (json['totalPendingAmount'] ?? 0).toDouble(),
      breakdown: (json['breakdown'] as List?)
              ?.map((e) => PendingRentBreakdown.fromJson(e))
              .toList() ??
          [],
    );
  }
}

class PendingRentBreakdown {
  final DateTime fromDate;
  final DateTime toDate;
  final double rentPerDay;
  final double amount;
  final String roomNumber;

  PendingRentBreakdown({
    required this.fromDate,
    required this.toDate,
    required this.rentPerDay,
    required this.amount,
    required this.roomNumber,
  });

  factory PendingRentBreakdown.fromJson(Map<String, dynamic> json) {
    return PendingRentBreakdown(
      fromDate: DateTime.parse(json['fromDate']),
      toDate: DateTime.parse(json['toDate']),
      rentPerDay: (json['rentPerDay'] ?? 0).toDouble(),
      amount: (json['amount'] ?? 0).toDouble(),
      roomNumber: json['roomNumber'] ?? '',
    );
  }
}

class CreatePaymentRequest {
  final String tenantId;
  final double amount;
  final DateTime? paymentDate;
  final DateTime paidUpto;
  final String paymentFrequencyCode;
  final String paymentModeCode;
  final String? notes;

  CreatePaymentRequest({
    required this.tenantId,
    required this.amount,
    this.paymentDate,
    required this.paidUpto,
    required this.paymentFrequencyCode,
    required this.paymentModeCode,
    this.notes,
  });

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{
      'tenantId': tenantId,
      'amount': amount,
      'paidUpto': paidUpto.toIso8601String(),
      'paymentFrequencyCode': paymentFrequencyCode,
      'paymentModeCode': paymentModeCode,
    };
    if (paymentDate != null) map['paymentDate'] = paymentDate!.toIso8601String();
    if (notes != null && notes!.isNotEmpty) map['notes'] = notes;
    return map;
  }
}
