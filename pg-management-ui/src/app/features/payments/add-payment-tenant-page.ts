import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { AddPayment } from './add-payment/add-payment';
import { MobileAddPayment } from './mobile-add-payment/mobile-add-payment';

@Component({
  selector: 'app-add-payment-tenant-page',
  standalone: true,
  imports: [AddPayment, MobileAddPayment],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-add-payment />
    } @else {
      <app-add-payment />
    }
  `
})
export class AddPaymentTenantPage {
  screen = inject(ScreenService);
}
