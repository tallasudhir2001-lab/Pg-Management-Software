interface PendingRentBreakdownDto {
  roomNumber: string;
  fromDate: string;   // ISO from backend
  toDate: string;
  rentPerDay: number;
  amount: number;
}

interface PendingStayUiDto {
  roomNumber: string;
  fromDate: Date;
  toDate: Date;
  amount: number;
}
