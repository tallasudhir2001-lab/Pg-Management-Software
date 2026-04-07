import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PaymentService } from '../services/payment-service';
import { PaymentHistoryDto } from '../models/paymets-history-dto';

@Component({
  selector: 'app-mobile-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-payments.html',
  styleUrl: './mobile-payments.css'
})
export class MobilePayments implements OnInit {
  private paymentService = inject(PaymentService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  payments: PaymentHistoryDto[] = [];
  loading = true;
  error = '';
  totalCount = 0;

  ngOnInit() {
    this.loadPayments();
  }

  loadPayments() {
    this.loading = true;
    this.error = '';

    this.paymentService.getPaymentHistory({ page: 1, pageSize: 50 }).subscribe({
      next: (result) => {
        this.payments = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load payments';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  addPayment() {
    this.router.navigate(['/payments/add']);
  }

  openPayment(payment: PaymentHistoryDto) {
    this.router.navigate(['/payments', payment.paymentId]);
  }

  getModeStyle(mode: string): { bg: string; color: string } {
    switch (mode?.toLowerCase()) {
      case 'upi': return { bg: '#F3E5F5', color: '#7B1FA2' };
      case 'cash': return { bg: '#E8F5E9', color: '#2E7D32' };
      case 'bank': return { bg: '#E3F2FD', color: '#1565C0' };
      case 'card': return { bg: '#FFF3E0', color: '#E65100' };
      default: return { bg: '#F5F5F5', color: '#64748b' };
    }
  }
}
