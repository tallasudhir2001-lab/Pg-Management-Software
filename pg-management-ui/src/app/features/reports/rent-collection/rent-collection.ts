import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { ReportService } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';
import { SendReportModal } from '../send-report-modal/send-report-modal';

export interface RentCollectionRow {
  tenantName: string;
  roomNumber: string;
  tenantPhone: string;
  expectedRent: number;
  amountPaid: number;
  lastPaymentDate: string | null;
  paymentMode: string | null;
  status: string;
}

export interface RentCollectionData {
  month: number;
  year: number;
  rows: RentCollectionRow[];
  totalExpected: number;
  totalCollected: number;
  totalPending: number;
}

@Component({
  selector: 'app-rent-collection',
  standalone: true,
  imports: [CommonModule, FormsModule, SendReportModal],
  templateUrl: './rent-collection.html',
  styleUrl: './rent-collection.css'
})
export class RentCollectionReport {
  fromDate: string;
  toDate: string;
  statusFilter = '';

  data: RentCollectionData | null = null;
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
    let p = new HttpParams().set('fromDate', this.fromDate).set('toDate', this.toDate);
    if (this.statusFilter) p = p.set('status', this.statusFilter);
    return p;
  }

  loadData(): void {
    this.isLoading = true;
    this.data = null;
    this.reportService.getReportData<RentCollectionData>('rent-collection/data', this.buildParams()).subscribe({
      next: d => { this.data = d; this.isLoading = false; this.cdr.detectChanges(); },
      error: err => { this.toastService.showError(err?.error || 'Failed to load data'); this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  downloadPdf(): void {
    this.isDownloading = true;
    this.reportService.generateReport('rent-collection', this.buildParams()).subscribe({
      next: blob => {
        this.isDownloading = false;
        this.cdr.detectChanges();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `rent-collection-${this.fromDate}.pdf`;
        a.click(); URL.revokeObjectURL(url);
      },
      error: err => { this.toastService.showError(err?.error || 'Failed to generate PDF'); this.isDownloading = false; this.cdr.detectChanges(); }
    });
  }

  getFilters(): Record<string, string> {
    const f: Record<string, string> = { fromDate: this.fromDate, toDate: this.toDate };
    if (this.statusFilter) f['status'] = this.statusFilter;
    return f;
  }

  statusClass(status: string): string {
    if (status === 'Paid') return 'badge badge-green';
    if (status === 'Partial') return 'badge badge-amber';
    return 'badge badge-red';
  }

  goBack(): void { this.router.navigate(['/reports']); }
}
