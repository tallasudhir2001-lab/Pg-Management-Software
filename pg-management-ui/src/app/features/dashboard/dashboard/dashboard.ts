import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BehaviorSubject, forkJoin, of, Subscription } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { DashboardService, DateRange } from '../services/dashboard-service';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { DashboardSummary } from '../models/dashboard-summary.model';
import { DashboardAlerts } from '../models/dashboard-alerts.model';
import { CollectionSummary } from '../models/collection-summary.model';
import { AuditCount } from '../../audit/models/audit-event.model';
import { TenantListDto } from '../../tenant/models/tenant-list-dto';
import { VacancyLoss } from '../models/vacancy-loss.model';
import { TodaySnapshot } from '../models/today-snapshot.model';

interface DashboardVm {
  summary: DashboardSummary;
  expenses: { totalExpenses: number };
  collection: CollectionSummary;
  overdueTenants: TenantListDto[];
}

const EMPTY_SUMMARY: DashboardSummary = {
  activeTenants: 0, totalTenants: 0, movedOutTenants: 0,
  occupiedBeds: 0, vacantBeds: 0, totalRooms: 0, monthlyRevenue: 0
};

const EMPTY_COLLECTION: CollectionSummary = {
  expectedRent: 0, collectedRent: 0, pendingRent: 0,
  collectionRate: 0, paidCount: 0, pendingCount: 0
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  private rangeSubject = new BehaviorSubject<string>('this-month');
  private previousRange = 'this-month';
  private subs: Subscription[] = [];

  selectedRange = 'this-month';
  showCustomPicker = false;
  customFrom = '';
  customTo = '';

  dismissedAlerts = new Set<string>();

  readonly ranges = [
    { key: 'this-month',    label: 'This Month' },
    { key: 'last-month',    label: 'Last Month' },
    { key: 'last-3-months', label: 'Last 3 Months' },
    { key: 'this-year',     label: 'This Year' },
  ];

  alerts: DashboardAlerts | null = null;
  auditCount: AuditCount | null = null;
  vacancyLoss: VacancyLoss | null = null;
  todaySnapshot: TodaySnapshot | null = null;
  vm: DashboardVm | null = null;
  loading = true;

  constructor(
    private dashboardService: DashboardService,
    private tenantService: Tenantservice,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Alerts
    this.subs.push(
      this.dashboardService.getAlerts().pipe(
        catchError(() => of({ movedOutWithPendingRent: 0, movedOutWithUnsettledAdvance: 0, activeWithPendingRent: 0, overdueExpectedCheckouts: 0, overdueBookings: 0 }))
      ).subscribe(data => {
        this.alerts = data;
        this.cdr.detectChanges();
      })
    );

    // Audit count
    this.subs.push(
      this.dashboardService.getUnreviewedAuditCount().pipe(
        catchError(() => of({ unreviewedCount: 0 }))
      ).subscribe(data => {
        this.auditCount = data;
        this.cdr.detectChanges();
      })
    );

    // Vacancy loss
    this.subs.push(
      this.dashboardService.getVacancyLoss().pipe(
        catchError(() => of(null))
      ).subscribe(data => {
        this.vacancyLoss = data;
        this.cdr.detectChanges();
      })
    );

    // Today snapshot
    this.subs.push(
      this.dashboardService.getTodaySnapshot().pipe(
        catchError(() => of({ todayCollection: 0, todayExpenses: 0 }))
      ).subscribe(data => {
        this.todaySnapshot = data;
        this.cdr.detectChanges();
      })
    );

    // Main VM
    this.subs.push(
      this.rangeSubject.pipe(
        switchMap(rangeKey => {
          this.loading = true;
          this.cdr.detectChanges();
          const range = this.getDateRange(rangeKey);
          return forkJoin({
            summary:        this.dashboardService.getSummary(range).pipe(catchError(() => of(EMPTY_SUMMARY))),
            expenses:       this.dashboardService.getExpensesSummary(range).pipe(catchError(() => of({ totalExpenses: 0 }))),
            collection:     this.dashboardService.getCollectionSummary(range).pipe(catchError(() => of(EMPTY_COLLECTION))),
            overdueTenants: this.tenantService.getTenants({
              page: 1, pageSize: 10,
              status: 'ACTIVE', rentPending: true,
              sortBy: 'daysoverdue', sortDir: 'desc'
            }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
          });
        }),
      ).subscribe(data => {
        this.vm = {
          ...data,
          overdueTenants: (data.overdueTenants as any).items as TenantListDto[]
        };
        this.loading = false;
        this.cdr.detectChanges();
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  // ── Range ─────────────────────────────────────────────────
  onRangeChange(key: string): void {
    if (key === 'custom') {
      if (this.selectedRange !== 'custom') {
        this.previousRange = this.selectedRange;
      }
      this.selectedRange = 'custom';
      this.showCustomPicker = true;
      return;
    }
    this.showCustomPicker = false;
    this.selectedRange = key;
    this.rangeSubject.next(key);
  }

  applyCustomRange(): void {
    if (!this.customFrom || !this.customTo) return;
    this.showCustomPicker = false;
    this.rangeSubject.next('custom');
  }

  cancelCustomPicker(): void {
    this.selectedRange = this.previousRange;
    this.showCustomPicker = false;
  }

  get customRangeLabel(): string {
    if (!this.customFrom || !this.customTo) return 'Custom';
    const fmt = (d: string) => new Date(d).toLocaleDateString('en-IN', { day: 'numeric', month: 'short' });
    return `${fmt(this.customFrom)} – ${fmt(this.customTo)}`;
  }

  // ── Alert helpers ──────────────────────────────────────────
  dismissAlert(key: string): void       { this.dismissedAlerts.add(key); }
  isAlertVisible(key: string): boolean  { return !this.dismissedAlerts.has(key); }

  // ── Alert navigation ───────────────────────────────────────
  goToMovedOutPendingRent():      void { this.router.navigate(['/tenant-list'], { queryParams: { status: 'MOVED OUT', rentPending: 'true' } }); }
  goToMovedOutUnsettledAdvance(): void { this.router.navigate(['/tenant-list'], { queryParams: { status: 'MOVED OUT', advancePending: 'true' } }); }
  goToActivePendingRent():        void { this.router.navigate(['/tenant-list'], { queryParams: { status: 'ACTIVE', rentPending: 'true' } }); }
  goToOverdueExpectedCheckouts(): void { this.router.navigate(['/tenant-list'], { queryParams: { status: 'ACTIVE', overdueCheckout: 'true' } }); }
  goToOverdueBookings():          void { this.router.navigate(['/bookings'], { queryParams: { status: 'Active', overdue: 'true' } }); }
  goToAuditLog():                 void { this.router.navigate(['/audit-log']); }

  goToTodayCollection(): void {
    const today = new Date().toISOString().split('T')[0];
    this.router.navigate(['/payments/history'], { queryParams: { from: today, to: today } });
  }

  goToTodayExpenses(): void {
    const today = new Date().toISOString().split('T')[0];
    this.router.navigate(['/expenses'], { queryParams: { from: today, to: today } });
  }

  // ── KPI helpers ────────────────────────────────────────────
  getNetProfit(vm: DashboardVm): number {
    return vm.summary.monthlyRevenue - vm.expenses.totalExpenses;
  }

  getOccupancyRate(vm: DashboardVm): number {
    const total = vm.summary.occupiedBeds + vm.summary.vacantBeds;
    return total === 0 ? 0 : Math.round((vm.summary.occupiedBeds / total) * 100);
  }

  getCollectionFill(vm: DashboardVm): string {
    return `${Math.min(vm.collection.collectionRate, 100)}%`;
  }

  // ── Navigation ─────────────────────────────────────────────
  navigateTo(path: string): void { this.router.navigate([path]); }
  viewTenant(id: string): void   { this.router.navigate(['/tenants', id]); }

  // ── Date range ─────────────────────────────────────────────
  private getDateRange(key: string): DateRange {
    const now = new Date();
    switch (key) {
      case 'this-month':    return { from: new Date(now.getFullYear(), now.getMonth(), 1), to: now };
      case 'last-month':    return { from: new Date(now.getFullYear(), now.getMonth() - 1, 1), to: new Date(now.getFullYear(), now.getMonth(), 0) };
      case 'last-3-months': return { from: new Date(now.getFullYear(), now.getMonth() - 2, 1), to: now };
      case 'this-year':     return { from: new Date(now.getFullYear(), 0, 1), to: now };
      case 'custom':        return { from: new Date(this.customFrom), to: new Date(this.customTo) };
      default:              return { from: new Date(now.getFullYear(), now.getMonth(), 1), to: now };
    }
  }
}
