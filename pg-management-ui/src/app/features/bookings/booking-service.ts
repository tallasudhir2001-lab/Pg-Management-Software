import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResults } from '../../shared/models/page-results.model';
import { BookingListItem, BookingDetails } from './models/booking.model';
import { CreateBookingDto } from './models/create-booking.dto';
import { BookingListQuery } from './models/booking-list-query.dto';

@Injectable({
  providedIn: 'root',
})
export class BookingService {
  private readonly baseUrl = `${environment.apiBaseUrl}/bookings`;

  constructor(private http: HttpClient) {}

  getBookings(params: BookingListQuery): Observable<PagedResults<BookingListItem>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.fromDate) {
      httpParams = httpParams.set('fromDate', params.fromDate);
    }
    if (params.toDate) {
      httpParams = httpParams.set('toDate', params.toDate);
    }
    if (params.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.roomId) {
      httpParams = httpParams.set('roomId', params.roomId);
    }
    if (params.sortBy) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }
    if (params.sortDir) {
      httpParams = httpParams.set('sortDir', params.sortDir);
    }

    return this.http.get<PagedResults<BookingListItem>>(this.baseUrl, {
      params: httpParams as any,
    });
  }

  getBookingById(bookingId: string): Observable<BookingDetails> {
    return this.http.get<BookingDetails>(`${this.baseUrl}/${bookingId}`);
  }

  createBooking(dto: CreateBookingDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/create-booking`, dto);
  }

  updateBooking(bookingId: string, dto: { roomId: string; scheduledCheckInDate: string; advanceAmount: number; notes: string | null }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/update-booking/${bookingId}`, dto);
  }

  cancelBooking(bookingId: string): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}/cancel-booking/${bookingId}`,
      {}
    );
  }

  terminateBooking(bookingId: string): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}/terminate-booking/${bookingId}`,
      {}
    );
  }

  checkActiveBooking(tenantId: string): Observable<{ hasActiveBooking: boolean }> {
    return this.http.get<{ hasActiveBooking: boolean }>(
      `${this.baseUrl}/check-active/${tenantId}`
    );
  }

  terminateNoShows(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/terminate-no-shows`, {});
  }
}