class RoomListItem {
  final String roomId;
  final String roomNumber;
  final int capacity;
  final int occupied;
  final int vacancies;
  final double rentAmount;
  final String status;
  final bool isAc;

  RoomListItem({
    required this.roomId,
    required this.roomNumber,
    required this.capacity,
    required this.occupied,
    required this.vacancies,
    required this.rentAmount,
    required this.status,
    required this.isAc,
  });

  factory RoomListItem.fromJson(Map<String, dynamic> json) {
    return RoomListItem(
      roomId: json['roomId'],
      roomNumber: json['roomNumber'] ?? '',
      capacity: json['capacity'] ?? 0,
      occupied: json['occupied'] ?? 0,
      vacancies: json['vacancies'] ?? 0,
      rentAmount: (json['rentAmount'] ?? 0).toDouble(),
      status: json['status'] ?? 'Available',
      isAc: json['isAc'] ?? false,
    );
  }
}
