class PaymentListItem {
  final String paymentId;
  final String tenantName;
  final String paymentType;
  final DateTime paymentDate;
  final String? periodCovered;
  final double amount;
  final String mode;
  final String? collectedBy;

  PaymentListItem({
    required this.paymentId,
    required this.tenantName,
    required this.paymentType,
    required this.paymentDate,
    this.periodCovered,
    required this.amount,
    required this.mode,
    this.collectedBy,
  });

  factory PaymentListItem.fromJson(Map<String, dynamic> json) {
    return PaymentListItem(
      paymentId: json['paymentId'],
      tenantName: json['tenantName'] ?? 'Unknown',
      paymentType: json['paymentType'] ?? '',
      paymentDate: DateTime.parse(json['paymentDate']),
      periodCovered: json['periodCovered'],
      amount: (json['amount'] ?? 0).toDouble(),
      mode: json['mode'] ?? '',
      collectedBy: json['collectedBy'],
    );
  }
}

class PaymentMode {
  final String code;
  final String description;

  PaymentMode({required this.code, required this.description});

  factory PaymentMode.fromJson(Map<String, dynamic> json) {
    return PaymentMode(
      code: json['code'],
      description: json['description'] ?? json['code'],
    );
  }
}

class PaymentType {
  final String code;
  final String name;

  PaymentType({required this.code, required this.name});

  factory PaymentType.fromJson(Map<String, dynamic> json) {
    return PaymentType(
      code: json['code'],
      name: json['name'] ?? json['code'],
    );
  }
}
