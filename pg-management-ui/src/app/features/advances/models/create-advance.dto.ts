export interface CreateAdvanceDto {
  tenantId: string;
  amount: number;
  paymentModeCode: string;
  notes?: string;
}