export interface CreatePaymentRequest {
  tenantId: string;
  PaymentFrequencyCode: 'MONTHLY' | 'DAILY' | 'CUSTOM';
  paidFrom: string;
  paidUpto: string;
  amount: number;
  paymentModeCode: string;
  notes?: string;
}
