export interface Advance {
  advanceId: string;
  amount: number;
  deductedAmount: number | null;
  isSettled: boolean;
  paidDate: string;
}