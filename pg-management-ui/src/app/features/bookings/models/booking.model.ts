export interface BookingListItem {
  bookingId: string;
  tenantId: string;
  tenantName: string;
  roomId: string;
  roomNumber: string;
  scheduledCheckInDate: string;
  status: string;
  advanceAmount: number;
  notes: string | null;
  createdAt: string;
}

export interface BookingDetails {
  bookingId: string;
  tenantId: string;
  tenantName: string;
  tenantContact: string;
  roomId: string;
  roomNumber: string;
  scheduledCheckInDate: string;
  status: string;
  advanceAmount: number;
  notes: string | null;
  createdAt: string;
  createdBy: string;
}