import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { BookingList } from './booking-list/booking-list';
import { MobileBookingList } from './mobile-booking-list/mobile-booking-list';

@Component({
  selector: 'app-booking-list-page',
  standalone: true,
  imports: [BookingList, MobileBookingList],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-booking-list />
    } @else {
      <app-booking-list />
    }
  `
})
export class BookingListPage {
  screen = inject(ScreenService);
}
