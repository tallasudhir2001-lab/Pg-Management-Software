import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface BookingConversionData {
  fromDate: string; toDate: string;
  totalBookings: number; checkedIn: number; cancelled: number;
  expired: number; stillActive: number; conversionRatePercent: number;
}

@Component({
  selector: 'app-booking-conversion',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './booking-conversion.html',
  styleUrl: './booking-conversion.css'
})
export class BookingConversionReport {
  fromDate: string;
  toDate: string;
  data: BookingConversionData | null = null;
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
    this.fromDate = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-01`;
    this.toDate = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
  }

  private buildParams(): HttpParams {
    return new HttpParams().set('fromDate', this.fromDate).set('toDate', this.toDate);
  }

  loadData(): void {
    this.isLoading = true; this.data = null;
    this.reportService.getReportData<BookingConversionData>('booking-conversion/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('booking-conversion', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false; this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `booking-conversion-${this.fromDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: () => { this.toastService.showError('Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  getFilters(): Record<string, string> { return { fromDate: this.fromDate, toDate: this.toDate }; }
  goBack(): void { this.router.navigate(['/reports']); }
}
