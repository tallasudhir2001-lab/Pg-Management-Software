import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { PaymentsLanding } from './payments-landing/payments-landing';
import { MobilePayments } from './mobile-payments/mobile-payments';

@Component({
  selector: 'app-payments-landing-page',
  standalone: true,
  imports: [PaymentsLanding, MobilePayments],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-payments />
    } @else {
      <app-payments-landing />
    }
  `
})
export class PaymentsLandingPage {
  screen = inject(ScreenService);
}
