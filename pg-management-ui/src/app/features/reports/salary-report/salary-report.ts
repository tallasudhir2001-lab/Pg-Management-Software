import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface SalaryRow {
  employeeName: string; role: string; paymentDate: string;
  forMonth: string; amount: number; paymentMode: string;
}
export interface SalaryReportData {
  month: number; year: number; rows: SalaryRow[];
  totalPaid: number; employeeCount: number;
}

@Component({
  selector: 'app-salary-report',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './salary-report.html',
  styleUrl: './salary-report.css'
})
export class SalaryReport {
  fromDate: string;
  data: SalaryReportData | null = null;
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
  }

  private buildParams(): HttpParams {
    return new HttpParams().set('fromDate', this.fromDate);
  }

  loadData(): void {
    this.isLoading = true; this.data = null;
    this.reportService.getReportData<SalaryReportData>('salary/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('salary', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false; this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `salary-report-${this.fromDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: () => { this.toastService.showError('Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  getFilters(): Record<string, string> { return { fromDate: this.fromDate }; }
  goBack(): void { this.router.navigate(['/reports']); }
}
