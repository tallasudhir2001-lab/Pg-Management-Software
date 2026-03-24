import { Advance } from "../../advances/models/advance.model";

export interface TenantDetailsModel {
  tenantId: string;

  // Tenant basic details
  name: string;
  contactNumber: string;
  aadharNumber: string;

  // Financial details
  hasAdvance: boolean;
  advanceAmount: number | null;
  advancePaymentMode: string | null;

  notes: string | null;

  // Room / stay details
  roomNumber: string | null;
  checkedInAt: string | null;
  movedOutAt: string | null;

  // Derived state
  status: 'ACTIVE' | 'MOVED OUT' | 'NO STAY';

  //stays
  stays: Stay[];

  advances: Advance[];
}
export interface Stay {
  roomId: string;
  roomNumber: string;
  fromDate: string;
  toDate: string | null;
}

