class ExpenseListItem {
  final String id;
  final DateTime expenseDate;
  final String category;
  final double amount;
  final String paymentMode;
  final String paymentModeLabel;
  final String description;

  ExpenseListItem({
    required this.id,
    required this.expenseDate,
    required this.category,
    required this.amount,
    required this.paymentMode,
    required this.paymentModeLabel,
    required this.description,
  });

  factory ExpenseListItem.fromJson(Map<String, dynamic> json) {
    return ExpenseListItem(
      id: json['id'],
      expenseDate: DateTime.parse(json['expenseDate']),
      category: json['category'] ?? '',
      amount: (json['amount'] ?? 0).toDouble(),
      paymentMode: json['paymentMode'] ?? '',
      paymentModeLabel: json['paymentModeLabel'] ?? json['paymentMode'] ?? '',
      description: json['description'] ?? '',
    );
  }
}

class ExpenseCategory {
  final int id;
  final String name;

  ExpenseCategory({required this.id, required this.name});

  factory ExpenseCategory.fromJson(Map<String, dynamic> json) {
    return ExpenseCategory(
      id: json['id'],
      name: json['name'] ?? '',
    );
  }
}

class CreateExpenseRequest {
  final int categoryId;
  final double amount;
  final DateTime expenseDate;
  final String description;
  final String paymentModeCode;
  final String? referenceNo;

  CreateExpenseRequest({
    required this.categoryId,
    required this.amount,
    required this.expenseDate,
    required this.description,
    required this.paymentModeCode,
    this.referenceNo,
  });

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{
      'categoryId': categoryId,
      'amount': amount,
      'expenseDate': expenseDate.toIso8601String(),
      'description': description,
      'paymentModeCode': paymentModeCode,
      'isRecurring': false,
    };
    if (referenceNo != null && referenceNo!.isNotEmpty) {
      map['referenceNo'] = referenceNo;
    }
    return map;
  }
}
