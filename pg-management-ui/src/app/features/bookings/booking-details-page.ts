import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { BookingDetails } from './booking-details/booking-details';
import { MobileBookingDetails } from './mobile-booking-details/mobile-booking-details';

@Component({
  selector: 'app-booking-details-page',
  standalone: true,
  imports: [BookingDetails, MobileBookingDetails],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-booking-details />
    } @else {
      <app-booking-details />
    }
  `
})
export class BookingDetailsPage {
  screen = inject(ScreenService);
}
