import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from '../services/dashboard-service';
import { DashboardSummary } from '../models/dashboard-summary.model';
import { RecentPayment } from '../models/recent-payment.model';

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

  summary: DashboardSummary | null = null;
  recentPayments: RecentPayment[] = [];
  loading = true;
  error = '';

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.error = '';

    this.dashboardService.getSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load dashboard';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });

    this.dashboardService.getRecentPayments(10).subscribe({
      next: (data) => { this.recentPayments = data; this.cdr.detectChanges(); },
      error: () => {}
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
}
