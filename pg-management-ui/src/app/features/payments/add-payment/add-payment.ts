import { Component, input, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
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

  constructor(
    private fb: FormBuilder,
    private paymentService: PaymentService,
    private route: ActivatedRoute,
    private router:Router,
    private toastService:ToastService
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
    this.form = this.fb.group({
      frequency: ['MONTHLY', Validators.required],
      paidFrom: [{ value: ctx.paidFrom, disabled: true }],
      paidUpto: [ctx.maxPaidUpto, Validators.required],
      amount: [ctx.pendingAmount, [Validators.required, Validators.min(1)]],
      paymentModeCode: [null, Validators.required],
      notes: ['']
    });

    this.form.get('frequency')!
      .valueChanges
      .subscribe(freq => this.updatePaidUpto(freq, ctx));
  }

  private updatePaidUpto(freq: string, ctx: PaymentContext) {
    const from = new Date(ctx.paidFrom);
    let upto = new Date(from);

    if (freq === 'MONTHLY') {
      upto = new Date(from.getFullYear(), from.getMonth() + 1, 0);
    } else if (freq === 'DAILY') {
      upto = new Date(from);
    }

    const max = new Date(ctx.maxPaidUpto);
    if (upto > max) {
      upto = max;
    }

    this.form.patchValue({ paidUpto: upto.toISOString().substring(0, 10) });
  }

  save(ctx: PaymentContext) {
    if (this.form.invalid) {
    this.toastService.showError('Please fill in all required fields correctly.');
    this.form.markAllAsTouched(); 
    return; 
  }
    if (this.saving) return;

    this.saving = true;

    const payload = {
      tenantId: ctx.tenantId,
      PaymentFrequencyCode:this.form.value.frequency,
      paidFrom: ctx.paidFrom,
      paidUpto: this.form.value.paidUpto,
      amount: this.form.value.amount,
      paymentModeCode: this.form.value.paymentModeCode,
      notes: this.form.value.notes
    };

    this.paymentService.createPayment(payload).subscribe({
      next: () => {
        this.saving = false;
        // close modal / navigate / emit event
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
