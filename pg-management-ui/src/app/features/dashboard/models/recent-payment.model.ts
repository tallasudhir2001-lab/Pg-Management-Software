export interface RecentPayment {
  tenantName: string;
  amount: number;
  paymentDate: string; // ISO string
  mode: string;
}
