export interface PaymentHistoryDto {
  paymentId: string;
  paymentDate: string; // ISO date string
  tenantId: string;
  tenantName: string;
  periodCovered: string; // e.g., "Jan 2024" or "Jan-Feb 2024"
  amount: number;
  mode: 'cash' | 'upi' | 'card' | 'bank';
  collectedBy: string;
  createdAt?: string;
  updatedAt?: string;
}
