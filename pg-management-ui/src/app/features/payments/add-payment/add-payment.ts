import { Component, ChangeDetectorRef, input, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Observable, tap } from 'rxjs';
import { PaymentService } from '../services/payment-service';
import { PaymentContext } from '../models/payment-context.model';
import { PaymentMode } from '../models/payment-mode.model';
import { DatePipe } from '@angular/common';
import { DecimalPipe } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink,RouterModule } from '@angular/router';
import { ToastService } from '../../../shared/toast/toast-service';

const PAYMENT_FREQUENCIES = [
  { key: 'MONTHLY', label: 'Monthly' },
  { key: 'DAILY', label: 'Daily' },
  { key: 'CUSTOM', label: 'Custom' }
];

@Component({
  selector: 'app-add-payment',
  standalone:true,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './add-payment.html',
  styleUrl: './add-payment.css',
})
export class AddPayment implements OnChanges {

  @Input() tenantId!: string;
  @Input() showHeader: boolean = true;

  paymentContext$!: Observable<PaymentContext>;
  paymentModes$!: Observable<PaymentMode[]>;

  form!: FormGroup;
  frequencies = PAYMENT_FREQUENCIES;

  saving = false;
  calculatingRent = false;

  constructor(
    private fb: FormBuilder,
    private paymentService: PaymentService,
    private route: ActivatedRoute,
    private router:Router,
    private toastService:ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Handle route-based usage
    if (!this.tenantId) {
      const routeTenantId = this.route.snapshot.paramMap.get('tenantId');
      if (routeTenantId) {
        this.tenantId = routeTenantId;
        this.loadContext();
      }
    }
  }
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['tenantId'] && this.tenantId) {
      this.loadContext();
    }
  }

  private loadContext() {
  this.paymentContext$ = this.paymentService
    .getPaymentContext(this.tenantId)
    .pipe(
      tap(ctx => this.buildForm(ctx))
    );

  this.paymentModes$ = this.paymentService.getPaymentModes();
}


  private buildForm(ctx: PaymentContext) {
    // Only build form if there's pending amount
    if (ctx.pendingAmount === 0) {
      return;
    }

    // Calculate initial paidUpto based on stay type and billing cycle
    const cycleDay = ctx.stayStartDate ? new Date(ctx.stayStartDate).getDate() : 1;
    const initialPaidUpto = this.calculatePaidUpto(ctx.paidFrom!, ctx.maxPaidUpto, ctx.stayType, cycleDay);

    this.form = this.fb.group({
      frequency: [ctx.stayType === 'DAILY' ? 'DAILY' : 'MONTHLY', Validators.required],
      paidFrom: [{ value: ctx.paidFrom, disabled: true }],
      paidUpto: [initialPaidUpto, Validators.required],
      amount: [ctx.pendingAmount, [Validators.required, Validators.min(1)]],
      paymentModeCode: [null, Validators.required],
      notes: ['']
    });

    this.form.get('frequency')!
      .valueChanges
      .subscribe(freq => this.onFrequencyChange(freq, ctx));

    // Auto-calculate amount when paidUpto changes
    this.form.get('paidUpto')!
      .valueChanges
      .subscribe(paidUpto => this.onPaidUptoChange(paidUpto, ctx));

    // Calculate initial amount based on the auto-populated paidUpto
    this.recalculateAmount(ctx.paidFrom!, initialPaidUpto);
  }

  /**
   * Calculate paidUpto based on billing cycle.
   * MONTHLY: cycle anchored on stayStartDate.day (e.g. 29th → 29 Mar–28 Apr, 29 Apr–28 May).
   * DAILY: same day as paidFrom.
   */
  private calculatePaidUpto(paidFrom: string, maxPaidUpto: string | null, stayType: string, cycleDay: number): string {
    const from = new Date(paidFrom);
    let upto: Date;

    if (stayType === 'MONTHLY') {
      upto = this.getCycleEnd(from, cycleDay);
    } else {
      // DAILY: same day
      upto = new Date(from);
    }

    // Cap at maxPaidUpto
    if (maxPaidUpto) {
      const max = new Date(maxPaidUpto);
      if (upto > max) {
        upto = max;
      }
    }

    // Format as YYYY-MM-DD using local date parts (not UTC) to avoid timezone shift
    const yy = upto.getFullYear();
    const mm = String(upto.getMonth() + 1).padStart(2, '0');
    const dd = String(upto.getDate()).padStart(2, '0');
    return `${yy}-${mm}-${dd}`;
  }

  /**
   * Get the end of the billing cycle that contains 'date'.
   * Cycle starts on cycleDay of some month, ends on cycleDay-1 of next month.
   * E.g. cycleDay=29: cycle is 29th → 28th of next month.
   * cycleDay=1: cycle is 1st → last day of same month.
   */
  private getCycleEnd(date: Date, cycleDay: number): Date {
    // Find the cycle start that contains this date
    const year = date.getFullYear();
    const month = date.getMonth();
    const day = date.getDate();

    let cycleStartYear: number, cycleStartMonth: number;
    const adjustedDay = Math.min(cycleDay, new Date(year, month + 1, 0).getDate());

    if (day >= adjustedDay) {
      cycleStartYear = year;
      cycleStartMonth = month;
    } else {
      // Cycle started previous month
      const prev = new Date(year, month - 1, 1);
      cycleStartYear = prev.getFullYear();
      cycleStartMonth = prev.getMonth();
    }

    // Next cycle start = cycleDay of next month from cycle start
    const nextMonth = cycleStartMonth + 1;
    const nextYear = cycleStartYear + Math.floor(nextMonth / 12);
    const nextMonthAdj = nextMonth % 12;
    const daysInNextMonth = new Date(nextYear, nextMonthAdj + 1, 0).getDate();
    const nextCycleDay = Math.min(cycleDay, daysInNextMonth);
    const nextCycleStart = new Date(nextYear, nextMonthAdj, nextCycleDay);

    // Cycle end = next cycle start - 1 day
    const cycleEnd = new Date(nextCycleStart);
    cycleEnd.setDate(cycleEnd.getDate() - 1);
    return cycleEnd;
  }

  private onFrequencyChange(freq: string, ctx: PaymentContext) {
    if (!ctx.paidFrom) return;

    const from = new Date(ctx.paidFrom);
    const cycleDay = ctx.stayStartDate ? new Date(ctx.stayStartDate).getDate() : 1;
    let upto: Date;

    if (freq === 'MONTHLY') {
      upto = this.getCycleEnd(from, cycleDay);
    } else if (freq === 'DAILY') {
      upto = new Date(from);
    } else {
      return; // CUSTOM — don't auto-set
    }

    if (ctx.maxPaidUpto) {
      const max = new Date(ctx.maxPaidUpto);
      if (upto > max) {
        upto = max;
      }
    }

    const yy = upto.getFullYear();
    const mm = String(upto.getMonth() + 1).padStart(2, '0');
    const dd = String(upto.getDate()).padStart(2, '0');
    this.form.patchValue({ paidUpto: `${yy}-${mm}-${dd}` });
    // amount will auto-update via paidUpto valueChanges subscription
  }

  private onPaidUptoChange(paidUpto: string, ctx: PaymentContext) {
    if (!ctx.paidFrom || !paidUpto) return;
    this.recalculateAmount(ctx.paidFrom, paidUpto);
  }

  private recalculateAmount(paidFrom: string, paidUpto: string) {
    this.calculatingRent = true;
    this.cdr.detectChanges();
    this.paymentService.calculateRent(this.tenantId, paidFrom, paidUpto).subscribe({
      next: (result) => {
        this.form.patchValue({ amount: result.amount }, { emitEvent: false });
        this.calculatingRent = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.calculatingRent = false;
        this.cdr.detectChanges();
      }
    });
  }

  save(ctx: PaymentContext) {
    if (this.form.invalid) {
      this.toastService.showError('Please fill in all required fields correctly.');
      this.form.markAllAsTouched(); 
      return; 
    }
    
    if (this.saving) return;

    // Validate that paidFrom exists
    if (!ctx.paidFrom) {
      this.toastService.showError('Payment cannot be processed - no pending amount.');
      return;
    }

    this.saving = true;

    const payload = {
      tenantId: ctx.tenantId,
      PaymentFrequencyCode: this.form.value.frequency,
      paidFrom: ctx.paidFrom, // TypeScript now knows this is not null
      paidUpto: this.form.value.paidUpto,
      amount: this.form.value.amount,
      paymentModeCode: this.form.value.paymentModeCode,
      notes: this.form.value.notes
    };

    this.paymentService.createPayment(payload).subscribe({
      next: () => {
        this.saving = false;
        this.toastService.showSuccess('Payment recorded successfully');
        this.router.navigate(['/tenants', ctx.tenantId]);
      },
      error: (err: { error: string; }) => {
        this.toastService.showError(err.error || "Failed to Add Payment");
        this.saving = false;
      }
    });
  }
}