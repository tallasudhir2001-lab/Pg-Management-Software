import { Component, ChangeDetectionStrategy } from '@angular/core';
import { forkJoin, Observable, BehaviorSubject } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { DashboardService } from '../services/dashboard-service';
import { DashboardSummary } from '../models/dashboard-summary.model';
import { RevenueTrend } from '../models/revenue-trend.model';
import { RecentPayment } from '../models/recent-payment.model';
import { Occupancy } from '../models/occupancy.model';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../../../shared/shared.module';

interface DashboardVm {
  summary: DashboardSummary;
  expenses: { totalExpenses: number };
  revenueTrend: RevenueTrend[];
  recentPayments: RecentPayment[];
  occupancy: Occupancy;
}

@Component({
  selector: 'app-dashboard',
  standalone:true,
  imports: [CommonModule,SharedModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  private rangeSubject = new BehaviorSubject<string>('this-month');
  selectedRange = 'this-month';

  ranges = [
    { key: 'this-month', label: 'This Month' },
    { key: 'last-month', label: 'Last Month' },
    { key: 'last-3-months', label: 'Last 3 Months' },
    { key: 'this-year', label: 'This Year' }
  ];

  onRangeChange(rangeKey: string) {
  this.selectedRange = rangeKey;
  this.rangeSubject.next(rangeKey);
}


  readonly year = new Date().getFullYear();
    vm$!: Observable<DashboardVm>;

  constructor(private dashboardService: DashboardService) {
    this.vm$ = this.rangeSubject.pipe(
  switchMap(rangeKey => {
    const range = this.getDateRange(rangeKey);

    return forkJoin({
      summary: this.dashboardService.getSummary(range),
      expenses: this.dashboardService.getExpensesSummary(range),
      revenueTrend: this.dashboardService.getRevenueTrend(range),
      recentPayments: this.dashboardService.getRecentPayments(5, range),
      occupancy: this.dashboardService.getOccupancy() // current state
    });
  })
);

  }
  private getDateRange(rangeKey: string): { from: Date; to: Date } {
  const now = new Date();

  switch (rangeKey) {
    case 'this-month':
      return {
        from: new Date(now.getFullYear(), now.getMonth(), 1),
        to: now
      };

    case 'last-month':
      return {
        from: new Date(now.getFullYear(), now.getMonth() - 1, 1),
        to: new Date(now.getFullYear(), now.getMonth(), 0)
      };

    case 'last-3-months':
      return {
        from: new Date(now.getFullYear(), now.getMonth() - 2, 1),
        to: now
      };

    case 'this-year':
      return {
        from: new Date(now.getFullYear(), 0, 1),
        to: now
      };

    default:
      return {
        from: new Date(now.getFullYear(), now.getMonth(), 1),
        to: now
      };
  }
}
getNetProfit(vm: DashboardVm): number {
  return vm.summary.monthlyRevenue - vm.expenses.totalExpenses;
}

}

