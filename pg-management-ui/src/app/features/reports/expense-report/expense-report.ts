import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface ExpenseRow {
  date: string;
  category: string;
  description: string;
  amount: number;
}

export interface ExpenseGroup {
  category: string;
  rows: ExpenseRow[];
  subtotal: number;
}

export interface ExpenseData {
  month: number;
  year: number;
  groups: ExpenseGroup[];
  grandTotal: number;
}

@Component({
  selector: 'app-expense-report',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './expense-report.html',
  styleUrl: './expense-report.css'
})
export class ExpenseReport {
  fromDate: string;
  toDate: string;
  categoryFilter = '';

  data: ExpenseData | null = null;
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
    let p = new HttpParams().set('fromDate', this.fromDate).set('toDate', this.toDate);
    if (this.categoryFilter) p = p.set('category', this.categoryFilter);
    return p;
  }

  loadData(): void {
    this.isLoading = true;
    this.data = null;
    this.reportService.getReportData<ExpenseData>('expenses/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('expenses', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `expense-report-${this.fromDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: err => { this.toastService.showError(err?.error || 'Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  getFilters(): Record<string, string> {
    const f: Record<string, string> = { fromDate: this.fromDate, toDate: this.toDate };
    if (this.categoryFilter) f['category'] = this.categoryFilter;
    return f;
  }

  get totalRows(): number {
    return this.data?.groups.reduce((s, g) => s + g.rows.length, 0) ?? 0;
  }

  goBack(): void { this.router.navigate(['/reports']); }
}
