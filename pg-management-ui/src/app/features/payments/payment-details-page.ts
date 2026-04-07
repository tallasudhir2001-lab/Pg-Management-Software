import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { PaymentDetails as PaymentDetailsComponent } from './payment-details/payment-details';
import { MobilePaymentDetails } from './mobile-payment-details/mobile-payment-details';

@Component({
  selector: 'app-payment-details-page',
  standalone: true,
  imports: [PaymentDetailsComponent, MobilePaymentDetails],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-payment-details />
    } @else {
      <app-payment-details />
    }
  `
})
export class PaymentDetailsPage {
  screen = inject(ScreenService);
}
