import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  SettingsService,
  ReportOption,
  UserReportSubscription,
  UpdateUserReportSubscriptions
} from '../services/settings.service';

@Component({
  selector: 'app-report-subscriptions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-subscriptions.html',
  styleUrl: './report-subscriptions.css'
})
export class ReportSubscriptions implements OnInit {
  loading = true;
  saving = false;
  successMsg = '';
  error = '';

  reportOptions: ReportOption[] = [];
  users: UserReportSubscription[] = [];

  // Track selections: userId -> Set of reportTypes
  selections: Map<string, Set<string>> = new Map();

  constructor(private settingsService: SettingsService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading = true;
    let loaded = 0;

    this.settingsService.getReportOptions().subscribe({
      next: opts => {
        this.reportOptions = opts;
        loaded++;
        if (loaded === 2) this.finishLoading();
      },
      error: () => { this.error = 'Failed to load report options.'; this.loading = false; this.cdr.detectChanges(); }
    });

    this.settingsService.getReportSubscriptions().subscribe({
      next: subs => {
        this.users = subs;
        this.selections.clear();
        for (const user of subs) {
          this.selections.set(user.userId, new Set(user.subscribedReports));
        }
        loaded++;
        if (loaded === 2) this.finishLoading();
      },
      error: () => { this.error = 'Failed to load subscriptions.'; this.loading = false; this.cdr.detectChanges(); }
    });
  }

  private finishLoading() {
    this.loading = false;
    this.error = '';
    this.cdr.detectChanges();
  }

  isChecked(userId: string, reportType: string): boolean {
    return this.selections.get(userId)?.has(reportType) ?? false;
  }

  toggle(userId: string, reportType: string) {
    const set = this.selections.get(userId);
    if (!set) return;
    if (set.has(reportType)) {
      set.delete(reportType);
    } else {
      set.add(reportType);
    }
    this.successMsg = '';
  }

  getSubscribedCount(userId: string): number {
    return this.selections.get(userId)?.size ?? 0;
  }

  save() {
    this.saving = true;
    this.error = '';
    this.successMsg = '';

    const userSubscriptions: UpdateUserReportSubscriptions[] = [];
    this.selections.forEach((reportTypes, userId) => {
      userSubscriptions.push({
        userId,
        reportTypes: Array.from(reportTypes)
      });
    });

    this.settingsService.updateReportSubscriptions(userSubscriptions).subscribe({
      next: () => {
        this.saving = false;
        this.successMsg = 'Report subscriptions saved successfully!';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.message || 'Failed to save subscriptions.';
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/settings']);
  }
}
