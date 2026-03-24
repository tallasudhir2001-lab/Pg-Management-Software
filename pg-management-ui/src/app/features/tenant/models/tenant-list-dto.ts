export interface TenantListDto {
  tenantId: string;
  name: string;

  roomId?: string | null;
  roomNumber?: string | null;

  contactNumber: string;
  status: 'ACTIVE' | 'MOVED OUT' | 'NO STAY';
  checkedInAt?: string | null;
  isRentPending: boolean;
}
