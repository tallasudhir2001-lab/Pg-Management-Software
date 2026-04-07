import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { Expenses } from './expenses/expenses';
import { MobileExpenses } from './mobile-expenses/mobile-expenses';

@Component({
  selector: 'app-expenses-page',
  standalone: true,
  imports: [Expenses, MobileExpenses],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-expenses />
    } @else {
      <app-expenses />
    }
  `
})
export class ExpensesPage {
  screen = inject(ScreenService);
}
