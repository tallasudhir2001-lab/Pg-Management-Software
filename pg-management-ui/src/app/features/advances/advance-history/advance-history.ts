import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Advance } from '../models/advance.model';
import { AdvanceService } from '../services/advance-service';
import { ToastService } from '../../../shared/toast/toast-service';
import { PaymentService } from '../../payments/services/payment-service';
import { PaymentMode } from '../../payments/models/payment-mode.model';
import { Observable } from 'rxjs';
import { SettleAdvanceModal } from '../settle-advance-modal/settle-advance-modal';

@Component({
  selector: 'app-advance-history',
  standalone: true,
  imports: [CommonModule, FormsModule, SettleAdvanceModal],
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
  isAddModalOpen = false;
  showAll = false;

  get visibleAdvances(): Advance[] {
    return this.showAll ? this.advances : this.advances.filter(a => !a.isSettled);
  }

  newAmount: number = 0;
  newPaymentMode: string = '';

  constructor(
    private advanceService: AdvanceService,
    private toastService: ToastService,
    private paymentService: PaymentService
  ) {}

  ngOnInit() {
    this.paymentModes$ = this.paymentService.getPaymentModes();
  }

  openSettleModal(a: Advance) {
    this.selectedAdvance = a;
    this.isModalOpen = true;
  }

  onAdvanceSettled() {
    this.isModalOpen = false;
    this.selectedAdvance = null;
    this.settled.emit();
  }

  onSettleModalClosed() {
    this.isModalOpen = false;
    this.selectedAdvance = null;
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