import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BookingService } from '../booking-service';
import { BookingListItem } from '../models/booking.model';

@Component({
  selector: 'app-mobile-booking-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-booking-list.html',
  styleUrl: './mobile-booking-list.css'
})
export class MobileBookingList implements OnInit {
  private bookingService = inject(BookingService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  bookings: BookingListItem[] = [];
  loading = true;
  error = '';
  activeFilter = 'Active';
  filters = ['All', 'Active', 'Cancelled', 'Terminated'];

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    this.bookingService.getBookings({
      page: 1,
      pageSize: 50,
      status: this.activeFilter === 'All' ? undefined : this.activeFilter,
      sortBy: 'createdAt',
      sortDir: 'desc'
    }).subscribe({
      next: (res) => {
        this.bookings = res.items;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load bookings';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  setFilter(f: string) {
    this.activeFilter = f;
    this.load();
  }

  openBooking(b: BookingListItem) {
    this.router.navigate(['/bookings', b.bookingId]);
  }

  addBooking() {
    this.router.navigate(['/bookings/add']);
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active': return 'status-active';
      case 'cancelled': return 'status-cancelled';
      case 'terminated': return 'status-terminated';
      default: return '';
    }
  }
}
