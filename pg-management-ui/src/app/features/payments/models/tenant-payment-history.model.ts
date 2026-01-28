export interface TenantPaymentHistory {
  paymentId: string;
  paymentDate: string;
  paidFrom: string;
  paidUpto: string;
  amount: number;
  paymentMode: string;
  frequency: string;
  collectedBy: string;
}