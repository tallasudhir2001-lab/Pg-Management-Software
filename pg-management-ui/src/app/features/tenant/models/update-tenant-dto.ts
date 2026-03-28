export interface UpdateTenantDto {
  name: string;
  contactNumber: string;
  aadharNumber: string;
  notes: string | null;
  email: string | null;
}
