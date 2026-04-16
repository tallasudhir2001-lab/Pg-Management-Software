// models/pending-rent.model.ts
export interface PendingRentBreakdown {
  fromDate: string;
  toDate: string;
  rentPerDay: number;
  amount: number;
  roomNumber: string;
}

export interface LastPayment {
  paymentDate: string;
  paidFrom: string;
  paidUpto: string;
  amount: number;
  paymentMode: string;
}

export interface PendingRent {
  tenantId: string;
  asOfDate: string;
  totalPendingAmount: number;
  breakdown: PendingRentBreakdown[];
  lastPayment: LastPayment | null;
}
