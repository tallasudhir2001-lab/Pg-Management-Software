import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Tenantservice } from '../services/tenantservice';
import { PaymentService } from '../../payments/services/payment-service';
import { AdvanceService } from '../../advances/services/advance-service';
import { TenantDetailsModel } from '../models/tenant-details.model';
import { PendingRent } from '../models/pending-rent.model';
import { TenantPaymentHistory } from '../../payments/models/tenant-payment-history.model';
import { Advance } from '../../advances/models/advance.model';
import { PaymentMode } from '../../payments/models/payment-mode.model';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-mobile-tenant-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mobile-tenant-details.html',
  styleUrl: './mobile-tenant-details.css'
})
export class MobileTenantDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private tenantService = inject(Tenantservice);
  private paymentService = inject(PaymentService);
  private advanceService = inject(AdvanceService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);

  tenant: TenantDetailsModel | null = null;
  pendingRent: PendingRent | null = null;
  payments: TenantPaymentHistory[] = [];
  loading = true;
  error = '';
  showPayments = false;
  showStays = false;
  showAdvances = false;

  // Settle advance
  settlingAdvance: Advance | null = null;
  settleDeductedAmount = 0;
  settlePaymentModeCode = '';
  settleLoading = false;
  paymentModes: PaymentMode[] = [];

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadTenant(id);
    this.paymentService.getPaymentModes().subscribe(modes => {
      this.paymentModes = modes;
      this.cdr.detectChanges();
    });
  }

  loadTenant(id: string) {
    this.loading = true;
    this.tenantService.getTenantById(id).subscribe({
      next: (tenant) => {
        this.tenant = tenant;
        this.loading = false;
        this.cdr.detectChanges();
        this.loadPendingRent(id);
        this.loadPayments(id);
      },
      error: () => {
        this.error = 'Failed to load tenant';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadPendingRent(id: string) {
    this.tenantService.getPendingRent(id).subscribe({
      next: (data) => { this.pendingRent = data; this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  loadPayments(id: string) {
    this.paymentService.getTenantPaymentHistory(id).subscribe({
      next: (data) => { this.payments = data; this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  goBack() {
    this.router.navigate(['/tenant-list']);
  }

  addPayment() {
    if (this.tenant) {
      this.router.navigate(['/payments/add', this.tenant.tenantId]);
    }
  }

  openPayment(p: TenantPaymentHistory) {
    this.router.navigate(['/payments', p.paymentId]);
  }

  togglePayments() {
    this.showPayments = !this.showPayments;
  }

  toggleStays() {
    this.showStays = !this.showStays;
  }

  toggleAdvances() {
    this.showAdvances = !this.showAdvances;
  }

  openSettle(adv: Advance) {
    this.settlingAdvance = adv;
    this.settleDeductedAmount = 0;
    this.settlePaymentModeCode = '';
  }

  closeSettle() {
    this.settlingAdvance = null;
  }

  get refundAmount(): number {
    if (!this.settlingAdvance) return 0;
    return this.settlingAdvance.amount - this.settleDeductedAmount;
  }

  confirmSettle() {
    if (!this.settlingAdvance) return;

    if (this.settleDeductedAmount < 0) {
      this.toastService.showError('Deduction cannot be negative');
      return;
    }
    if (this.settleDeductedAmount > this.settlingAdvance.amount) {
      this.toastService.showError('Deduction cannot exceed advance');
      return;
    }
    if (!this.settlePaymentModeCode && this.refundAmount > 0) {
      this.toastService.showError('Select payment mode for refund');
      return;
    }

    this.settleLoading = true;
    this.advanceService.settleAdvance(this.settlingAdvance.advanceId, {
      deductedAmount: this.settleDeductedAmount,
      paymentModeCode: this.settlePaymentModeCode
    }).subscribe({
      next: () => {
        this.settleLoading = false;
        this.toastService.showSuccess('Advance settled successfully');
        this.settlingAdvance = null;
        // Reload tenant to refresh advances
        if (this.tenant) this.loadTenant(this.tenant.tenantId);
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.settleLoading = false;
        this.toastService.showError(err?.error || 'Failed to settle advance');
        this.cdr.detectChanges();
      }
    });
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'ACTIVE': return '#2E7D32';
      case 'MOVED OUT': return '#64748b';
      default: return '#F57F17';
    }
  }
}
