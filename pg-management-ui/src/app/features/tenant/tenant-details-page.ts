import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { TenantDetails } from './tenant-details/tenant-details';
import { MobileTenantDetails } from './mobile-tenant-details/mobile-tenant-details';

@Component({
  selector: 'app-tenant-details-page',
  standalone: true,
  imports: [TenantDetails, MobileTenantDetails],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-tenant-details />
    } @else {
      <app-tenant-details />
    }
  `
})
export class TenantDetailsPage {
  screen = inject(ScreenService);
}
