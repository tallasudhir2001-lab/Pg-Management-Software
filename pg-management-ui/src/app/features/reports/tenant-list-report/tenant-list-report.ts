import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface TenantListRow {
  tenantName: string;
  phone: string;
  aadhaarMasked: string;
  roomNumber: string | null;
  checkInDate: string | null;
  moveOutDate: string | null;
  status: string;
  monthlyRent: number;
}

export interface TenantListData {
  statusFilter: string;
  rows: TenantListRow[];
}

@Component({
  selector: 'app-tenant-list-report',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './tenant-list-report.html',
  styleUrl: './tenant-list-report.css'
})
export class TenantListReport {
  statusFilter = 'ACTIVE';

  data: TenantListData | null = null;
  isLoading = false;
  isDownloading = false;
  showSendModal = false;

  constructor(
    private reportService: ReportService,
    private toastService: ToastService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  private buildParams(): HttpParams {
    return new HttpParams().set('status', this.statusFilter);
  }

  loadData(): void {
    this.isLoading = true;
    this.data = null;
    this.reportService.getReportData<TenantListData>('tenant-list/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('tenant-list', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `tenant-list-${this.statusFilter.toLowerCase()}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: err => { this.toastService.showError(err?.error || 'Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  getFilters(): Record<string, string> {
    return { status: this.statusFilter };
  }

  statusClass(status: string): string {
    if (status === 'ACTIVE') return 'badge badge-green';
    if (status === 'MOVED OUT') return 'badge badge-gray';
    return 'badge badge-blue';
  }

  goBack(): void { this.router.navigate(['/reports']); }
}
