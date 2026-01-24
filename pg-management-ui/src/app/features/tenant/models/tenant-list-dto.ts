export interface TenantListDto {
  tenantId: string;
  name: string;

  roomId?: string | null;
  roomNumber?: string | null;

  contactNumber: string;
  status: 'Active' | 'MovedOut';
  checkedInAt?: string | null;
}
