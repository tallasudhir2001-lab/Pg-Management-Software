import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { TenantList } from './tenant-list/tenant-list';
import { MobileTenantList } from './mobile-tenant-list/mobile-tenant-list';

@Component({
  selector: 'app-tenant-list-page',
  standalone: true,
  imports: [TenantList, MobileTenantList],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-tenant-list />
    } @else {
      <app-tenant-list />
    }
  `
})
export class TenantListPage {
  screen = inject(ScreenService);
}
