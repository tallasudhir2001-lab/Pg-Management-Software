import { Component, EventEmitter, Input, OnInit, Output, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService, ReportRecipient } from '../services/report.service';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-send-report-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './send-report-modal.html',
  styleUrl: './send-report-modal.css'
})
export class SendReportModal implements OnInit {
  @Input() reportType = '';
  @Input() filters: Record<string, string> = {};
  @Output() closed = new EventEmitter<void>();

  recipients: ReportRecipient[] = [];
  loadingRecipients = true;

  selectedRecipient: ReportRecipient | null = null;
  customEmail = '';
  customPhone = '';
  useCustomEmail = false;
  useCustomPhone = false;

  isSendingEmail = false;
  isSendingWhatsApp = false;

  constructor(
    private reportService: ReportService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.reportService.getAvailableRecipients().subscribe({
      next: (r) => {
        this.recipients = r;
        this.loadingRecipients = false;
        if (r.length > 0) this.selectedRecipient = r[0];
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingRecipients = false;
        this.cdr.detectChanges();
      }
    });
  }

  get resolvedEmail(): string {
    if (this.useCustomEmail) return this.customEmail.trim();
    return this.selectedRecipient?.email || '';
  }

  get resolvedPhone(): string {
    if (this.useCustomPhone) return this.customPhone.trim();
    return this.selectedRecipient?.phoneNumber || '';
  }

  selectRecipient(r: ReportRecipient): void {
    this.selectedRecipient = r;
    this.useCustomEmail = false;
    this.useCustomPhone = false;
  }

  sendEmail(): void {
    const email = this.resolvedEmail;
    if (!email) {
      this.toastService.showError('Please select a recipient or enter an email');
      return;
    }
    this.isSendingEmail = true;
    this.reportService.sendReport(this.reportType, email, this.filters).subscribe({
      next: () => {
        this.isSendingEmail = false;
        this.toastService.showSuccess('Report sent via Email');
        this.closed.emit();
      },
      error: (err) => {
        this.isSendingEmail = false;
        this.toastService.showError(err?.error?.message || err?.error || 'Failed to send email');
        this.cdr.detectChanges();
      }
    });
  }

  sendWhatsApp(): void {
    const phone = this.resolvedPhone;
    if (!phone) {
      this.toastService.showError('Please select a recipient with a phone number or enter one');
      return;
    }
    this.isSendingWhatsApp = true;
    this.reportService.sendReportWhatsApp(this.reportType, phone, this.filters).subscribe({
      next: () => {
        this.isSendingWhatsApp = false;
        this.toastService.showSuccess('Report sent via WhatsApp');
        this.closed.emit();
      },
      error: (err) => {
        this.isSendingWhatsApp = false;
        this.toastService.showError(err?.error?.message || err?.error || 'Failed to send via WhatsApp');
        this.cdr.detectChanges();
      }
    });
  }

  close(): void {
    this.closed.emit();
  }
}
