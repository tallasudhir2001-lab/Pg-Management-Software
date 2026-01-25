export interface TenantDetailsModel {
  tenantId: string;

  // Tenant basic details
  name: string;
  contactNumber: string;
  aadharNumber: string;

  // Financial details
  advanceAmount: number;
  rentPaidUpto: string | null;
  notes: string | null;

  // Room / stay details
  roomNumber: string | null;
  checkedInAt: string | null;

  // Derived state
  status: 'Active' | 'MovedOut';
}
