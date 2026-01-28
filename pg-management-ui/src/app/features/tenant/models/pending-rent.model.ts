// models/pending-rent.model.ts
export interface PendingRentBreakdown {
  fromDate: string;
  toDate: string;
  rentPerDay: number;
  amount: number;
  roomNumber: string;
}

export interface PendingRent {
  tenantId: string;
  asOfDate: string;
  totalPendingAmount: number;
  breakdown: PendingRentBreakdown[];
}
