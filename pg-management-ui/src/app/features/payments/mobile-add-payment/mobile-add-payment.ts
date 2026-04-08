import { Component, inject, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { TenantListDto } from '../../tenant/models/tenant-list-dto';
import { PaymentService } from '../services/payment-service';
import { PaymentContext } from '../models/payment-context.model';
import { PaymentMode } from '../models/payment-mode.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, Subscription } from 'rxjs';

const PAYMENT_FREQUENCIES = [
  { key: 'MONTHLY', label: 'Monthly' },
  { key: 'DAILY', label: 'Daily' },
  { key: 'CUSTOM', label: 'Custom' }
];

@Component({
  selector: 'app-mobile-add-payment',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, DecimalPipe],
  templateUrl: './mobile-add-payment.html',
  styleUrl: './mobile-add-payment.css'
})
export class MobileAddPayment implements OnInit, OnDestroy {
  private tenantService = inject(Tenantservice);
  private paymentService = inject(PaymentService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  // Tenant search
  searchText = '';
  searchResults: TenantListDto[] = [];
  selectedTenant: TenantListDto | null = null;
  searching = false;

  // Payment context
  paymentContext: PaymentContext | null = null;
  paymentModes: PaymentMode[] = [];
  loadingContext = false;
  frequencies = PAYMENT_FREQUENCIES;

  // Form fields
  frequency = 'MONTHLY';
  paidFrom = '';
  paidUpto = '';
  amount: number | null = null;
  paymentModeCode = '';
  notes = '';

  saving = false;
  calculatingRent = false;

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  ngOnInit() {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(text => {
        if (text.length < 3) { this.searchResults = []; return of(null); }
        this.searching = true;
        return this.tenantService.getTenants({ page: 1, pageSize: 8, search: text, status: 'ACTIVE' });
      })
    ).subscribe({
      next: (result) => {
        if (result) this.searchResults = result.items;
        this.searching = false;
        this.cdr.detectChanges();
      },
      error: () => { this.searching = false; this.cdr.detectChanges(); }
    });

    this.paymentService.getPaymentModes().subscribe(modes => {
      this.paymentModes = modes;
      this.cdr.detectChanges();
    });

    const tenantId = this.route.snapshot.paramMap.get('tenantId');
    if (tenantId) {
      this.selectedTenant = { tenantId, name: 'Loading...', contactNumber: '', status: 'ACTIVE', isRentPending: false } as TenantListDto;
      this.loadPaymentContext(tenantId);
    }
  }

  ngOnDestroy() {
    this.searchSub?.unsubscribe();
  }

  onSearch() {
    this.searchSubject.next(this.searchText);
  }

  selectTenant(tenant: TenantListDto) {
    this.selectedTenant = tenant;
    this.searchResults = [];
    this.searchText = '';
    this.loadPaymentContext(tenant.tenantId);
  }

  clearTenant() {
    this.selectedTenant = null;
    this.paymentContext = null;
    this.searchResults = [];
    this.searchText = '';
    this.resetForm();
  }

  goBack() {
    this.router.navigate(['/payments/history']);
  }

  private loadPaymentContext(tenantId: string) {
    this.loadingContext = true;
    this.paymentContext = null;
    this.resetForm();
    this.cdr.detectChanges();

    this.paymentService.getPaymentContext(tenantId).subscribe({
      next: (ctx) => {
        this.paymentContext = ctx;
        // Update tenant name if we only had a placeholder
        if (this.selectedTenant && this.selectedTenant.name === 'Loading...') {
          this.selectedTenant = { ...this.selectedTenant, name: ctx.tenantName } as TenantListDto;
        }
        this.loadingContext = false;
        this.buildFormFromContext(ctx);
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingContext = false;
        this.toastService.showError('Failed to load payment context');
        this.cdr.detectChanges();
      }
    });
  }

  private buildFormFromContext(ctx: PaymentContext) {
    if (ctx.pendingAmount === 0 || !ctx.paidFrom) return;

    this.frequency = ctx.stayType === 'DAILY' ? 'DAILY' : 'MONTHLY';
    this.paidFrom = ctx.paidFrom;

    const cycleDay = ctx.stayStartDate ? new Date(ctx.stayStartDate).getDate() : 1;
    this.paidUpto = this.calculatePaidUpto(ctx.paidFrom, ctx.maxPaidUpto, ctx.stayType, cycleDay);
    this.amount = ctx.pendingAmount;

    // Recalculate amount from API
    this.recalculateAmount(ctx.paidFrom, this.paidUpto);
  }

  private resetForm() {
    this.frequency = 'MONTHLY';
    this.paidFrom = '';
    this.paidUpto = '';
    this.amount = null;
    this.paymentModeCode = '';
    this.notes = '';
  }

  onFrequencyChange() {
    const ctx = this.paymentContext;
    if (!ctx?.paidFrom) return;

    const from = new Date(ctx.paidFrom);
    const cycleDay = ctx.stayStartDate ? new Date(ctx.stayStartDate).getDate() : 1;
    let upto: Date;

    if (this.frequency === 'MONTHLY') {
      upto = this.getCycleEnd(from, cycleDay);
    } else if (this.frequency === 'DAILY') {
      upto = new Date(from);
    } else {
      return; // CUSTOM — don't auto-set
    }

    if (ctx.maxPaidUpto) {
      const max = new Date(ctx.maxPaidUpto);
      if (upto > max) upto = max;
    }

    this.paidUpto = this.formatDate(upto);
    this.recalculateAmount(ctx.paidFrom, this.paidUpto);
  }

  onPaidUptoChange() {
    const ctx = this.paymentContext;
    if (!ctx?.paidFrom || !this.paidUpto) return;
    this.recalculateAmount(ctx.paidFrom, this.paidUpto);
  }

  private recalculateAmount(paidFrom: string, paidUpto: string) {
    if (!this.selectedTenant) return;
    this.calculatingRent = true;
    this.cdr.detectChanges();

    this.paymentService.calculateRent(this.selectedTenant.tenantId, paidFrom, paidUpto).subscribe({
      next: (result) => {
        this.amount = result.amount;
        this.calculatingRent = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.calculatingRent = false;
        this.cdr.detectChanges();
      }
    });
  }

  private calculatePaidUpto(paidFrom: string, maxPaidUpto: string | null, stayType: string, cycleDay: number): string {
    const from = new Date(paidFrom);
    let upto: Date;

    if (stayType === 'MONTHLY') {
      upto = this.getCycleEnd(from, cycleDay);
    } else {
      upto = new Date(from);
    }

    if (maxPaidUpto) {
      const max = new Date(maxPaidUpto);
      if (upto > max) upto = max;
    }

    return this.formatDate(upto);
  }

  private getCycleEnd(date: Date, cycleDay: number): Date {
    const year = date.getFullYear();
    const month = date.getMonth();
    const day = date.getDate();

    let cycleStartYear: number, cycleStartMonth: number;
    const adjustedDay = Math.min(cycleDay, new Date(year, month + 1, 0).getDate());

    if (day >= adjustedDay) {
      cycleStartYear = year;
      cycleStartMonth = month;
    } else {
      const prev = new Date(year, month - 1, 1);
      cycleStartYear = prev.getFullYear();
      cycleStartMonth = prev.getMonth();
    }

    const nextMonth = cycleStartMonth + 1;
    const nextYear = cycleStartYear + Math.floor(nextMonth / 12);
    const nextMonthAdj = nextMonth % 12;
    const daysInNextMonth = new Date(nextYear, nextMonthAdj + 1, 0).getDate();
    const nextCycleDay = Math.min(cycleDay, daysInNextMonth);
    const nextCycleStart = new Date(nextYear, nextMonthAdj, nextCycleDay);

    const cycleEnd = new Date(nextCycleStart);
    cycleEnd.setDate(cycleEnd.getDate() - 1);
    return cycleEnd;
  }

  private formatDate(d: Date): string {
    const yy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yy}-${mm}-${dd}`;
  }

  get isFormValid(): boolean {
    return !!this.paidUpto && !!this.amount && this.amount > 0 && !!this.paymentModeCode;
  }

  save() {
    const ctx = this.paymentContext;
    if (!ctx || !ctx.paidFrom || !this.selectedTenant) return;

    if (!this.isFormValid) {
      this.toastService.showError('Please fill in all required fields.');
      return;
    }

    if (this.saving) return;
    this.saving = true;

    const payload = {
      tenantId: ctx.tenantId,
      PaymentFrequencyCode: this.frequency as 'MONTHLY' | 'DAILY' | 'CUSTOM',
      paidFrom: ctx.paidFrom,
      paidUpto: this.paidUpto,
      amount: this.amount!,
      paymentModeCode: this.paymentModeCode,
      notes: this.notes || undefined
    };

    this.paymentService.createPayment(payload).subscribe({
      next: () => {
        this.saving = false;
        this.toastService.showSuccess('Payment recorded successfully');
        this.router.navigate(['/tenants', ctx.tenantId]);
      },
      error: (err: { error: string }) => {
        this.toastService.showError(err.error || 'Failed to add payment');
        this.saving = false;
        this.cdr.detectChanges();
      }
    });
  }
}
