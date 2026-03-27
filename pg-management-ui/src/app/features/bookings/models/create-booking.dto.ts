export interface CreateBookingDto {
  aadharNumber: string;
  name: string;
  contactNumber: string;
  roomId: string;
  scheduledCheckInDate: string;
  advanceAmount: number | null;
  paymentModeCode: string | null;
  notes: string | null;
}