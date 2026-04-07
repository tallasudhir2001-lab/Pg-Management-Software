import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { Dashboard } from './dashboard/dashboard';
import { MobileDashboard } from './mobile-dashboard/mobile-dashboard';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [Dashboard, MobileDashboard],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-dashboard />
    } @else {
      <app-dashboard />
    }
  `
})
export class DashboardPage {
  screen = inject(ScreenService);
}
