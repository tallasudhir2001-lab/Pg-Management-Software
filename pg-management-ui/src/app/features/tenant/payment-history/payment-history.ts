import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { PaymentService } from '../../payments/services/payment-service';
import { TenantPaymentHistory } from '../../payments/models/tenant-payment-history.model';
import { DecimalPipe } from '@angular/common';
import { DatePipe } from '@angular/common';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-payment-history',
  imports: [DatePipe,DecimalPipe,CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './payment-history.html',
  styleUrl: './payment-history.css',
})
export class PaymentHistory implements OnInit{
  @Input() tenantId!: string;

  payments$!: Observable<TenantPaymentHistory[]>;

  // filter checkboxes — all selected by default
  showRentPayment = true;
  showAdvancePayment = true;
  showAdvanceRefund = true;

  constructor(private paymentService: PaymentService) {}

  ngOnInit(): void {
    this.payments$ = this.paymentService.getTenantPaymentHistory(this.tenantId);
  }

  filterPayments(payments: TenantPaymentHistory[]): TenantPaymentHistory[] {
    return payments.filter(p => {
      const type = p.paymentType?.toLowerCase() || '';
      if (type.includes('rent') && !this.showRentPayment) return false;
      if (type.includes('advance') && type.includes('refund') && !this.showAdvanceRefund) return false;
      if (type.includes('advance') && !type.includes('refund') && !this.showAdvancePayment) return false;
      return true;
    });
  }
}
