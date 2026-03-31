import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface PaymentHistoryReportRow {
  paymentDate: string;
  receiptNumber: string;
  tenantName: string;
  roomNumber: string | null;
  paymentType: string;
  paymentMode: string;
  amount: number;
  notes: string | null;
}

export interface PaymentHistoryReportData {
  fromDate: string;
  toDate: string;
  rows: PaymentHistoryReportRow[];
  totalAmount: number;
}

@Component({
  selector: 'app-payment-history-report',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './payment-history-report.html',
  styleUrl: './payment-history-report.css'
})
export class PaymentHistoryReport {
  fromDate: string;
  toDate: string;

  data: PaymentHistoryReportData | null = null;
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
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
    this.toDate = now.toISOString().split('T')[0];
  }

  private buildParams(): HttpParams {
    return new HttpParams().set('fromDate', this.fromDate).set('toDate', this.toDate);
  }

  loadData(): void {
    this.isLoading = true;
    this.data = null;
    this.reportService.getReportData<PaymentHistoryReportData>('payment-history/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('payment-history', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `payment-history-${this.fromDate}-to-${this.toDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: err => { this.toastService.showError(err?.error || 'Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  getFilters(): Record<string, string> {
    return { fromDate: this.fromDate, toDate: this.toDate };
  }

  goBack(): void { this.router.navigate(['/reports']); }
}
