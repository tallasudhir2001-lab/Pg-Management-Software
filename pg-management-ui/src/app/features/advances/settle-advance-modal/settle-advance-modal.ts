import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Advance } from '../models/advance.model';
import { AdvanceService } from '../services/advance-service';
import { ToastService } from '../../../shared/toast/toast-service';
import { PaymentService } from '../../payments/services/payment-service';
import { PaymentMode } from '../../payments/models/payment-mode.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-settle-advance-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settle-advance-modal.html',
  styleUrls: ['./settle-advance-modal.css']
})
export class SettleAdvanceModal implements OnInit, OnChanges {
  @Input() advance: Advance | null = null;
  @Input() show: boolean = false;
  @Output() settled = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  paymentModes$!: Observable<PaymentMode[]>;
  deductedAmount = 0;
  paymentModeCode = '';
  loading = false;

  constructor(
    private advanceService: AdvanceService,
    private paymentService: PaymentService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.paymentModes$ = this.paymentService.getPaymentModes();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['show'] && this.show) {
      this.deductedAmount = 0;
      this.paymentModeCode = '';
    }
  }

  close(): void {
    this.closed.emit();
  }

  settle(): void {
    if (!this.advance) return;

    if (this.deductedAmount < 0) {
      this.toastService.showError('Deduction cannot be negative');
      return;
    }

    if (this.deductedAmount > this.advance.amount) {
      this.toastService.showError('Deduction cannot exceed advance');
      return;
    }

    if (!this.paymentModeCode && (this.advance.amount - this.deductedAmount) > 0) {
      this.toastService.showError('Select payment mode for refund');
      return;
    }

    this.loading = true;

    this.advanceService.settleAdvance(this.advance.advanceId, {
      deductedAmount: this.deductedAmount,
      paymentModeCode: this.paymentModeCode
    }).subscribe({
      next: () => {
        this.loading = false;
        this.toastService.showSuccess('Advance settled successfully');
        this.settled.emit();
      },
      error: (err: any) => {
        this.loading = false;
        const msg = err?.error?.message ?? err?.error ?? 'Failed to settle advance';
        this.toastService.showError(typeof msg === 'string' ? msg : 'Failed to settle advance');
      }
    });
  }
}
