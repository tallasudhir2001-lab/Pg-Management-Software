export interface UpdateTenantDto {
  name: string;
  contactNumber: string;
  aadharNumber: string;
  advanceAmount: number;
  rentPaidUpto: string | null;
  notes: string | null;
}
