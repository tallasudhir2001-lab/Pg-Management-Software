export interface PaymentContext {
  tenantId: string;
  PaymentFrequencyCode: 'MONTHLY' | 'DAILY' | 'CUSTOM';
  tenantName: string;
  paidFrom: string;
  maxPaidUpto: string;
  pendingAmount: number;
  asOfDate: string;
  hasActiveStay: boolean;
}
