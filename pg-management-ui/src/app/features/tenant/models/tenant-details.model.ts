export interface TenantDetailsModel {
  tenantId: string;

  // Tenant basic details
  name: string;
  contactNumber: string;
  aadharNumber: string;

  // Financial details
  advanceAmount: number;
  notes: string | null;

  // Room / stay details
  roomNumber: string | null;
  checkedInAt: string | null;
  movedOutAt: string | null;

  // Derived state
  status: 'Active' | 'MovedOut';
}
