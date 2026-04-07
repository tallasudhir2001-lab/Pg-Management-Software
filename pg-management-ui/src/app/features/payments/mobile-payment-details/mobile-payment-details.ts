import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../services/payment-service';

interface PaymentDetailsData {
  paymentId: string;
  tenantId: string;
  paymentDate: string;
  paidFrom: string;
  paidUpto: string;
  amount: number;
  paymentModeCode: string;
  paymentFrequencyCode: string;
  notes: string;
}

@Component({
  selector: 'app-mobile-payment-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-payment-details.html',
  styleUrl: './mobile-payment-details.css'
})
export class MobilePaymentDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private paymentService = inject(PaymentService);
  private cdr = inject(ChangeDetectorRef);

  payment: PaymentDetailsData | null = null;
  loading = true;
  error = '';

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('paymentId');
    if (id) this.loadPayment(id);
  }

  loadPayment(id: string) {
    this.loading = true;
    this.paymentService.getPayment(id).subscribe({
      next: (data: PaymentDetailsData) => {
        this.payment = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load payment';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/payments/history']);
  }

  viewTenant() {
    if (this.payment) {
      this.router.navigate(['/tenants', this.payment.tenantId]);
    }
  }

  getModeLabel(code: string): string {
    const modes: Record<string, string> = { cash: 'Cash', upi: 'UPI', bank: 'Bank Transfer', card: 'Card' };
    return modes[code?.toLowerCase()] || code;
  }

  getFreqLabel(code: string): string {
    const freqs: Record<string, string> = { MONTHLY: 'Monthly', DAILY: 'Daily', CUSTOM: 'Custom', ONETIME: 'One Time' };
    return freqs[code] || code;
  }
}
