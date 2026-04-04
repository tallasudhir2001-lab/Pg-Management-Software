import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../services/payment-service';
import { PaymentHistoryDto } from '../models/paymets-history-dto';
import { PagedResults } from '../../../shared/models/page-results.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { ReportService } from '../../reports/services/report.service';
import { SendReportModal } from '../../reports/send-report-modal/send-report-modal';
import { HttpParams } from '@angular/common/http';

interface PaymentTypeFilter {
  code: string;
  label: string;
  checked: boolean;
}

@Component({
  selector: 'app-rent-collection-view',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './rent-collection-view.html',
  styleUrl: './rent-collection-view.css'
})
export class RentCollectionView {
  fromDate: string;
  toDate: string;
  paymentTypeFilters: PaymentTypeFilter[] = [
    { code: 'RENT', label: 'Rent', checked: true },
    { code: 'ADV_PAYMENT', label: 'Advance Payment', checked: false },
    { code: 'ADV_REFUND', label: 'Advance Refund', checked: false }
  ];

  data: PagedResults<PaymentHistoryDto> | null = null;
  isLoading = false;
  isDownloading = false;
  showSendModal = false;

  constructor(
    private paymentService: PaymentService,
    private reportService: ReportService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    const today = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    this.fromDate = today;
    this.toDate = today;
  }

  get selectedTypes(): string[] {
    return this.paymentTypeFilters.filter(f => f.checked).map(f => f.code);
  }

  get hasAtLeastOneType(): boolean {
    return this.paymentTypeFilters.some(f => f.checked);
  }

  loadData(): void {
    if (!this.hasAtLeastOneType) {
      this.toastService.showError('Please select at least one payment type');
      return;
    }
    this.isLoading = true;
    this.data = null;

    this.paymentService.getPaymentHistory({
      page: 1,
      pageSize: 500,
      types: this.selectedTypes,
      fromDate: this.fromDate,
      toDate: this.toDate,
      sortBy: 'paymentDate',
      sortDir: 'desc'
    }).subscribe({
      next: result => {
        this.data = result;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: err => {
        this.toastService.showError(err?.error || 'Failed to load collection data');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    const params = new HttpParams()
      .set('fromDate', this.fromDate)
      .set('toDate', this.toDate);

    this.reportService.generateReport('rent-collection', params).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `rent-collection-${this.fromDate}-to-${this.toDate}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: err => {
        this.toastService.showError(err?.error || 'Failed to generate PDF');
        this.isDownloading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getFilters(): Record<string, string> {
    return { fromDate: this.fromDate, toDate: this.toDate };
  }

  formatCurrency(amount: number): string {
    return '₹' + amount.toLocaleString('en-IN');
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatPaymentMode(mode: string): string {
    const map: Record<string, string> = { cash: 'Cash', upi: 'UPI', card: 'Card', bank: 'Bank' };
    return map[mode.toLowerCase()] || mode;
  }
}
