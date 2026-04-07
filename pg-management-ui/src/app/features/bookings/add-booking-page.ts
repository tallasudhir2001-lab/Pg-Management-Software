import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { AddBooking } from './add-booking/add-booking';
import { MobileAddBooking } from './mobile-add-booking/mobile-add-booking';

@Component({
  selector: 'app-add-booking-page',
  standalone: true,
  imports: [AddBooking, MobileAddBooking],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-add-booking />
    } @else {
      <app-add-booking />
    }
  `
})
export class AddBookingPage {
  screen = inject(ScreenService);
}
