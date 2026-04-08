import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DashboardService } from '../services/dashboard-service';
import { DashboardSummary } from '../models/dashboard-summary.model';
import { DashboardAlerts } from '../models/dashboard-alerts.model';
import { TodaySnapshot } from '../models/today-snapshot.model';
import { CollectionSummary } from '../models/collection-summary.model';

@Component({
  selector: 'app-mobile-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-dashboard.html',
  styleUrl: './mobile-dashboard.css'
})
export class MobileDashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  summary: DashboardSummary | null = null;
  alerts: DashboardAlerts | null = null;
  today: TodaySnapshot | null = null;
  collection: CollectionSummary | null = null;
  loading = true;
  error = '';

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.error = '';

    forkJoin({
      summary: this.dashboardService.getSummary(),
      alerts: this.dashboardService.getAlerts(),
      today: this.dashboardService.getTodaySnapshot(),
      collection: this.dashboardService.getCollectionSummary()
    }).subscribe({
      next: (data) => {
        this.summary = data.summary;
        this.alerts = data.alerts;
        this.today = data.today;
        this.collection = data.collection;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load dashboard';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get occupancyPercent(): number {
    if (!this.summary) return 0;
    const total = this.summary.occupiedBeds + this.summary.vacantBeds;
    return total > 0 ? Math.round((this.summary.occupiedBeds / total) * 100) : 0;
  }

  get occupancyColor(): string {
    const pct = this.occupancyPercent;
    if (pct >= 80) return '#C62828';
    if (pct >= 50) return '#F57F17';
    return '#2E7D32';
  }

  get totalAlerts(): number {
    if (!this.alerts) return 0;
    return this.alerts.activeWithPendingRent + this.alerts.overdueExpectedCheckouts
      + this.alerts.overdueBookings + this.alerts.movedOutWithPendingRent
      + this.alerts.movedOutWithUnsettledAdvance;
  }

  navigate(path: string) {
    this.router.navigate([path]);
  }
}
