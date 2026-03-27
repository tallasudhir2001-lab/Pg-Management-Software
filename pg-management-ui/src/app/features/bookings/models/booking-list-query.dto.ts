export interface BookingListQuery {
  page: number;
  pageSize: number;
  fromDate?: string;
  toDate?: string;
  status?: string;
  roomId?: string;
  sortBy?: string;
  sortDir?: string;
}