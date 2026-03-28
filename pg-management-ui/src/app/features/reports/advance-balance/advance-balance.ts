import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';

export interface AdvanceBalanceRow {
  tenantName: string;
  roomNumber: string | null;
  advancePaid: number;
  advanceRefunded: number;
  balance: number;
  status: string;
}

export interface AdvanceBalanceData {
  rows: AdvanceBalanceRow[];
  totalHeld: number;
  totalRefunded: number;
  netBalance: number;
}

@Component({
  selector: 'app-advance-balance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './advance-balance.html',
  styleUrl: './advance-balance.css'
})
export class AdvanceBalanceReport {
  data: AdvanceBalanceData | null = null;
  isLoading = false;
  isDownloading = false;
  isSending = false;
  showEmailModal = false;
  recipientEmail = '';

  constructor(
    private reportService: ReportService,
    private toastService: ToastService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  loadData(): void {
    this.isLoading = true;
    this.data = null;
    this.reportService.getReportData<AdvanceBalanceData>('advance-balance/data', new HttpParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('advance-balance', new HttpParams()).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `advance-balance.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: err => { this.toastService.showError(err?.error || 'Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  openEmailModal(): void { this.recipientEmail = ''; this.showEmailModal = true; }

  sendEmail(): void {
    if (!this.recipientEmail) return;
    this.isSending = true;
    this.reportService.sendReport('advance-balance', this.recipientEmail).subscribe({
      next: () => { this.isSending = false; this.showEmailModal = false; this.toastService.showSuccess('Report sent successfully'); },
      error: err => { this.toastService.showError(err?.error || 'Failed to send'); this.isSending = false; }
    });
  }

  statusClass(status: string): string {
    if (status === 'Held') return 'badge badge-amber';
    if (status === 'Fully Refunded') return 'badge badge-green';
    return 'badge badge-blue';
  }

  goBack(): void { this.router.navigate(['/reports']); }
}
