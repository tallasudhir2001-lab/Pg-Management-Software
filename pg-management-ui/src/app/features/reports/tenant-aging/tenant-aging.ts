import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface TenantAgingBucket { bucket: string; count: number; totalAmount: number; }
export interface TenantAgingRow {
  tenantName: string; roomNumber: string; pendingAmount: number;
  daysOverdue: number; bucket: string;
}
export interface TenantAgingData {
  asOfDate: string; buckets: TenantAgingBucket[];
  details: TenantAgingRow[]; grandTotal: number;
}

@Component({
  selector: 'app-tenant-aging',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './tenant-aging.html',
  styleUrl: './tenant-aging.css'
})
export class TenantAgingReport {
  asOfDate: string;
  data: TenantAgingData | null = null;
  isLoading = false;
  isDownloading = false;
  showSendModal = false;

  constructor(
    private reportService: ReportService,
    private toastService: ToastService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    this.asOfDate = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
  }

  private buildParams(): HttpParams {
    return new HttpParams().set('asOfDate', this.asOfDate);
  }

  loadData(): void {
    this.isLoading = true; this.data = null;
    this.reportService.getReportData<TenantAgingData>('tenant-aging/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('tenant-aging', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false; this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `tenant-aging-${this.asOfDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: () => { this.toastService.showError('Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  bucketClass(bucket: string): string {
    if (bucket === '0-7 days') return 'badge-green';
    if (bucket === '8-15 days') return 'badge-amber';
    return 'badge-red';
  }

  getFilters(): Record<string, string> { return { asOfDate: this.asOfDate }; }
  goBack(): void { this.router.navigate(['/reports']); }
}
