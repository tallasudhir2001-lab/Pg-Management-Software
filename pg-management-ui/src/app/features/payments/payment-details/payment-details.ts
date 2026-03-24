import { Component, Input, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Observable, tap } from 'rxjs';
import { PaymentService } from '../services/payment-service';
import { PaymentMode } from '../models/payment-mode.model';
import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../shared/toast/toast-service';
import { TimeHelper } from '../../../shared/utils/time.helper';

export interface PaymentDetails {
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

const PAYMENT_FREQUENCIES = [
  { key: 'MONTHLY', label: 'Monthly' },
  { key: 'DAILY', label: 'Daily' },
  { key: 'CUSTOM', label: 'Custom' },
  { key: 'ONETIME',label: 'One Time'}
];

@Component({
  selector: 'app-payment-details',
  standalone: true,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './payment-details.html',
  styleUrls: ['../styles/payment-shared.css','./payment-details.css']
})
export class PaymentDetails implements OnChanges {

  @Input() paymentId!: string;
  @Input() mode: 'view' | 'edit' = 'view';
  @Input() showHeader: boolean = true;

  payment$!: Observable<PaymentDetails>;
  paymentModes$!: Observable<PaymentMode[]>;

  form!: FormGroup;
  frequencies = PAYMENT_FREQUENCIES;
  saving = false;
  loading = true;

  constructor(
    private fb: FormBuilder,
    private paymentService: PaymentService,
    private route: ActivatedRoute,
    private router: Router,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Handle route-based usage
    if (!this.paymentId) {
      const routePaymentId = this.route.snapshot.paramMap.get('paymentId');
      if (routePaymentId) {
        this.paymentId = routePaymentId;
        // Detect mode from route: /payments/:id => view, /payments/:id/edit => edit
        const urlSegments = this.route.snapshot.url.map(s => s.path);
        this.mode = urlSegments.includes('edit') ? 'edit' : 'view';
        this.loadPayment();
      }
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['paymentId'] && this.paymentId) || changes['mode']) {
      this.loadPayment();
    }
  }

  private loadPayment() {
    this.loading = true;
    this.payment$ = this.paymentService
      .getPayment(this.paymentId)
      .pipe(
        tap(payment => {
          this.buildForm(payment);
          this.loading = false;
          this.cdr.detectChanges();
        })
      );

    this.paymentModes$ = this.paymentService.getPaymentModes();
  }

  private buildForm(payment: PaymentDetails) {
    const isDisabled = this.mode === 'view';

    const paidFrom = TimeHelper.formatDateForInput(payment.paidFrom);
    const paidUpto = TimeHelper.formatDateForInput(payment.paidUpto);

    this.form = this.fb.group({
      frequency:       [{ value: payment.paymentFrequencyCode, disabled: isDisabled }, Validators.required],
      paidFrom:        [{ value: paidFrom,             disabled: true }],
      paidUpto:        [{ value: paidUpto,             disabled: isDisabled }, Validators.required],
      amount:          [{ value: payment.amount,               disabled: isDisabled }, [Validators.required, Validators.min(1)]],
      paymentModeCode: [{ value: payment.paymentModeCode,      disabled: isDisabled }, Validators.required],
      notes:           [{ value: payment.notes,                disabled: isDisabled }],
    });
  }

  get isEditMode(): boolean {
    return this.mode === 'edit';
  }

  enableEdit(): void {
    this.mode = 'edit';
    this.form.enable();
    this.form.get('paidFrom')?.disable(); // always keep paidFrom locked
    this.cdr.detectChanges();
  }

  save(payment: PaymentDetails) {
    if (this.form.invalid) {
      this.toastService.showError('Please fill in all required fields correctly.');
      this.form.markAllAsTouched();
      return;
    }

    if (this.saving) return;
    this.saving = true;

    const paidFrom = TimeHelper.formatDateForInput(payment.paidFrom);
    const paidUpto = TimeHelper.formatDateForInput(this.form.value.paidUpto);

    const payload = {
      paymentId: payment.paymentId,
      tenantId: payment.tenantId,
      paymentFrequencyCode: this.form.value.frequency,
      paidFrom: paidFrom,
      paidUpto: paidUpto,
      amount: this.form.value.amount,
      paymentModeCode: this.form.value.paymentModeCode,
      notes: this.form.value.notes
    };

    this.paymentService.updatePayment(payment.paymentId,payload).subscribe({
      next: () => {
        this.saving = false;
        this.toastService.showSuccess('Payment updated successfully');
        this.router.navigate(['/tenants', payment.tenantId]);
      },
      error: (err: { error: string }) => {
        this.toastService.showError(err.error || 'Failed to update payment');
        this.loadPayment();
        this.saving = false;
      }
    });
  }
}