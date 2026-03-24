import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Advance } from '../models/advance.model';
import { AdvanceService } from '../services/advance-service';
import { ToastService } from '../../../shared/toast/toast-service';
import { PaymentService } from '../../payments/services/payment-service';
import { PaymentMode } from '../../payments/models/payment-mode.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-advance-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './advance-history.html',
  styleUrls: ['./advance-history.css']
})
export class AdvanceHistory {

  @Input() advances: Advance[] = [];
  @Input() tenantId!: string;
  @Output() settled = new EventEmitter<void>();

  paymentModes$!: Observable<PaymentMode[]>;

  selectedAdvance: Advance | null = null;
  isModalOpen = false;

  deductedAmount: number = 0;
  paymentModeCode: string = '';

  isAddModalOpen = false;

  newAmount: number = 0;
  newPaymentMode: string = '';

  loading = false;

  constructor(
    private advanceService: AdvanceService,
    private toastService: ToastService,
    private paymentService:PaymentService
  ) {
  }
  ngOnInit() {
  this.paymentModes$ = this.paymentService.getPaymentModes();
}
  //  Open modal
  openSettleModal(a: Advance) {
    this.selectedAdvance = a;
    this.deductedAmount = 0;
    this.paymentModeCode = '';
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.selectedAdvance = null;
  }

  //  Submit settlement
  settle() {
    if (!this.selectedAdvance) return;

    if (this.deductedAmount < 0) {
      this.toastService.showError('Deduction cannot be negative');
      return;
    }

    if (this.deductedAmount > this.selectedAdvance.amount) {
      this.toastService.showError('Deduction cannot exceed advance');
      return;
    }

    if (!this.paymentModeCode && (this.selectedAdvance.amount - this.deductedAmount) > 0) {
      this.toastService.showError('Select payment mode for refund');
      return;
    }

    this.loading = true;

    this.advanceService.settleAdvance(this.selectedAdvance.advanceId, {
      deductedAmount: this.deductedAmount,
      paymentModeCode: this.paymentModeCode
    }).subscribe({
      next: () => {
        this.loading = false;
        this.toastService.showSuccess('Advance settled successfully');
        this.closeModal();
        this.settled.emit(); //  notify parent
      },
      error: err => {
        this.loading = false;
        this.toastService.showError(err?.error || 'Failed to settle advance');
      }
    });
  }
  createAdvance() {

  if (!this.newAmount || this.newAmount <= 0) {
    this.toastService.showError('Enter valid amount');
    return;
  }

  if (!this.newPaymentMode) {
    this.toastService.showError('Select payment mode');
    return;
  }

  this.advanceService.createAdvance({
    tenantId: this.tenantId, //  pass from parent
    amount: this.newAmount,
    paymentModeCode: this.newPaymentMode
  }).subscribe({
    next: () => {
      this.toastService.showSuccess('Advance added');
      this.closeAddModal();
      this.settled.emit(); // reuse refresh event
    },
    error: (err: { error: any; }) => {
      this.toastService.showError(err?.error || 'Failed to add advance');
    }
  });
}
  // helper
  getReturnAmount(a: Advance): number {
    return a.amount - (a.deductedAmount || 0);
  }

  get hasActiveAdvance(): boolean {
    return this.advances?.some(a => !a.isSettled);
  }
  openAddModal() {
    this.newAmount = 0;
    this.newPaymentMode = '';
    this.isAddModalOpen = true;
  }

  closeAddModal() {
    this.isAddModalOpen = false;
  }
}