import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ReportService } from '../../reports/services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-receipt-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './receipt-drawer.html',
  styleUrl: './receipt-drawer.css'
})
export class ReceiptDrawer implements OnChanges {
  @Input() paymentId: string | null = null;
  @Input() tenantEmail: string | null = null;
  @Output() closed = new EventEmitter<void>();

  pdfUrl: SafeResourceUrl | null = null;
  isLoading = false;
  isSending = false;
  recipientEmail = '';
  showEmailInput = false;

  constructor(
    private reportService: ReportService,
    private sanitizer: DomSanitizer,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['paymentId'] && this.paymentId) {
      this.loadReceipt();
      this.recipientEmail = this.tenantEmail ?? '';
      this.showEmailInput = false;
      this.pdfUrl = null;
    }
  }

  private loadReceipt(): void {
    if (!this.paymentId) return;
    this.isLoading = true;
    this.reportService.getReceipt(this.paymentId).subscribe({
      next: blob => {
        this.isLoading = false;
        const url = URL.createObjectURL(blob);
        this.pdfUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
        this.cdr.detectChanges();
      },
      error: err => {
        this.toastService.showError(err?.error || 'Failed to load receipt');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  print(): void {
    if (!this.paymentId) return;
    this.reportService.getReceipt(this.paymentId).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const win = window.open(url);
        win?.addEventListener('load', () => { win.print(); });
      },
      error: err => this.toastService.showError(err?.error || 'Failed to print receipt')
    });
  }

  sendReceipt(): void {
    if (!this.paymentId || !this.recipientEmail) return;
    this.isSending = true;
    this.reportService.sendReceipt(this.paymentId, this.recipientEmail).subscribe({
      next: () => {
        this.isSending = false;
        this.showEmailInput = false;
        this.toastService.showSuccess('Receipt sent successfully');
      },
      error: err => {
        this.toastService.showError(err?.error || 'Failed to send receipt');
        this.isSending = false;
      }
    });
  }

  close(): void {
    this.closed.emit();
  }
}
