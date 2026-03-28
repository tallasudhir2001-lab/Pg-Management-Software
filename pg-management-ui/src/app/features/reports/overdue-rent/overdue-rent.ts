import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';

export interface OverdueRentRow {
  tenantName: string;
  roomNumber: string;
  tenantPhone: string;
  lastPaymentDate: string | null;
  paidUpTo: string | null;
  overdueSince: string;
  daysOverdue: number;
  outstandingAmount: number;
}

export interface OverdueRentData {
  asOfDate: string;
  rows: OverdueRentRow[];
  totalOverdueTenants: number;
  totalOutstanding: number;
}

@Component({
  selector: 'app-overdue-rent',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './overdue-rent.html',
  styleUrl: './overdue-rent.css'
})
export class OverdueRentReport {
  asOfDate: string;

  data: OverdueRentData | null = null;
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
  ) {
    this.asOfDate = new Date().toISOString().split('T')[0];
  }

  private buildParams(): HttpParams {
    return new HttpParams().set('asOfDate', this.asOfDate);
  }

  loadData(): void {
    this.isLoading = true;
    this.data = null;
    this.reportService.getReportData<OverdueRentData>('overdue-rent/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('overdue-rent', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `overdue-rent-${this.asOfDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: err => { this.toastService.showError(err?.error || 'Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  openEmailModal(): void { this.recipientEmail = ''; this.showEmailModal = true; }

  sendEmail(): void {
    if (!this.recipientEmail) return;
    this.isSending = true;
    this.reportService.sendReport('overdue-rent', this.recipientEmail, { asOfDate: this.asOfDate }).subscribe({
      next: () => { this.isSending = false; this.showEmailModal = false; this.toastService.showSuccess('Report sent successfully'); },
      error: err => { this.toastService.showError(err?.error || 'Failed to send'); this.isSending = false; }
    });
  }

  goBack(): void { this.router.navigate(['/reports']); }
}
