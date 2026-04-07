import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { AddTenant } from './add-tenant/add-tenant';
import { MobileAddTenant } from './mobile-add-tenant/mobile-add-tenant';

@Component({
  selector: 'app-add-tenant-page',
  standalone: true,
  imports: [AddTenant, MobileAddTenant],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-add-tenant />
    } @else {
      <app-add-tenant />
    }
  `
})
export class AddTenantPage {
  screen = inject(ScreenService);
}
