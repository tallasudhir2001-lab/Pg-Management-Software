export interface PendingStayContext {
  roomId: string;
  roomNumber: string;
  fromDate: string;
  toDate: string;
  pendingAmount: number;
  isActiveStay: boolean;
  isNextPayable: boolean;
  rentPerMonth: number;
}

export interface PaymentContext {
  tenantId: string;
  tenantName: string;
  paidFrom: string | null;
  maxPaidUpto: string | null;
  pendingAmount: number;
  asOfDate: string;
  hasActiveStay: boolean;
  pendingStays: PendingStayContext[];
  roomNumber: string | null;
  rentPerMonth: number;
}