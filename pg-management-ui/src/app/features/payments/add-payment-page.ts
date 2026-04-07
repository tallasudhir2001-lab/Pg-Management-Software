import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { AddPaymentContainer } from './add-payment-container/add-payment-container';
import { MobileAddPayment } from './mobile-add-payment/mobile-add-payment';

@Component({
  selector: 'app-add-payment-page',
  standalone: true,
  imports: [AddPaymentContainer, MobileAddPayment],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-add-payment />
    } @else {
      <app-add-payment-container />
    }
  `
})
export class AddPaymentPage {
  screen = inject(ScreenService);
}
