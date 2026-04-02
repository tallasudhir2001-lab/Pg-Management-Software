export interface TenantListDto {
  tenantId: string;
  name: string;

  roomId?: string | null;
  roomNumber?: string | null;

  contactNumber: string;
  status: 'ACTIVE' | 'MOVED OUT' | 'NO STAY';
  checkedInAt?: string | null;
  isRentPending: boolean;
  stayType?: string | null;
  lastPaymentDate?: string | null;
  overdueSince?: string | null;
  daysOverdue?: number | null;
}
