class DashboardSummary {
  final int totalRooms;
  final int totalTenants;
  final int activeTenants;
  final int movedOutTenants;
  final int occupiedBeds;
  final int vacantBeds;
  final double monthlyRevenue;

  DashboardSummary({
    required this.totalRooms,
    required this.totalTenants,
    required this.activeTenants,
    required this.movedOutTenants,
    required this.occupiedBeds,
    required this.vacantBeds,
    required this.monthlyRevenue,
  });

  factory DashboardSummary.fromJson(Map<String, dynamic> json) {
    return DashboardSummary(
      totalRooms: json['totalRooms'] ?? 0,
      totalTenants: json['totalTenants'] ?? 0,
      activeTenants: json['activeTenants'] ?? 0,
      movedOutTenants: json['movedOutTenants'] ?? 0,
      occupiedBeds: json['occupiedBeds'] ?? 0,
      vacantBeds: json['vacantBeds'] ?? 0,
      monthlyRevenue: (json['monthlyRevenue'] ?? 0).toDouble(),
    );
  }

  double get occupancyRate =>
      (occupiedBeds + vacantBeds) > 0
          ? (occupiedBeds / (occupiedBeds + vacantBeds)) * 100
          : 0;
}

class RecentPayment {
  final String tenantName;
  final double amount;
  final DateTime paymentDate;
  final String mode;

  RecentPayment({
    required this.tenantName,
    required this.amount,
    required this.paymentDate,
    required this.mode,
  });

  factory RecentPayment.fromJson(Map<String, dynamic> json) {
    return RecentPayment(
      tenantName: json['tenantName'] ?? '',
      amount: (json['amount'] ?? 0).toDouble(),
      paymentDate: DateTime.parse(json['paymentDate']),
      mode: json['mode'] ?? '',
    );
  }
}

class OccupancyData {
  final int occupied;
  final int vacant;

  OccupancyData({required this.occupied, required this.vacant});

  factory OccupancyData.fromJson(Map<String, dynamic> json) {
    return OccupancyData(
      occupied: json['occupied'] ?? 0,
      vacant: json['vacant'] ?? 0,
    );
  }
}

class RevenueTrend {
  final String month;
  final double amount;

  RevenueTrend({required this.month, required this.amount});

  factory RevenueTrend.fromJson(Map<String, dynamic> json) {
    return RevenueTrend(
      month: json['month'] ?? '',
      amount: (json['amount'] ?? 0).toDouble(),
    );
  }
}
