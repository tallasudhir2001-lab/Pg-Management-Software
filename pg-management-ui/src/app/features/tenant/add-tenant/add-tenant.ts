import { Component, OnInit } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Room } from '../../rooms/models/room.model';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Roomservice } from '../../rooms/services/roomservice';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Tenantservice } from '../services/tenantservice';
import { ToastService } from '../../../shared/toast/toast-service';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { PaymentService } from '../../payments/services/payment-service';


@Component({
  selector: 'app-add-tenant',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './add-tenant.html',
  styleUrl: '../styles/tenant-form.css',
})
export class AddTenant implements OnInit {
  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private roomService: Roomservice,
    private router: Router,
    private tenantService: Tenantservice,
    private paymentService: PaymentService,
    private toastService: ToastService
  ) {}

  rooms$!: Observable<Room[]>;
  paymentModes$!: Observable<any[]>;
  error = '';
  existingTenant: any = null;
  checkingAadhar = false;

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', Validators.required],
      contactNumber: ['', Validators.required],
      aadharNumber: ['', [Validators.pattern(/^\d{12}$/)]],
      email: ['', [Validators.email]],
      hasRoom: [false],
      roomId: [''],
      fromDate: [null],
      hasAdvance: [false],
      advanceAmount: [null],
      paymentModeCode: [null],
      hasPayment: [false],
      initialPaymentAmount: [null],
      initialPaymentModeCode: [''],
      initialPaidFrom: [null],
      initialPaidUpto: [null],
      notes: ['']
    });

    this.loadAvailableRooms();
    this.paymentModes$ = this.paymentService.getPaymentModes();

    this.form.get('aadharNumber')?.valueChanges
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap(value => {
          if (!value || value.length !== 12) {
            this.existingTenant = null;
            return of(null);
          }
          this.checkingAadhar = true;
          return this.tenantService.findByAadhar(value).pipe(catchError(() => of(null)));
        })
      )
      .subscribe(res => {
        this.checkingAadhar = false;
        this.existingTenant = res || null;
      });
  }

  private loadAvailableRooms(): void {
    this.rooms$ = this.roomService.getRooms({ page: 1, pageSize: 100 }).pipe(map(res => res.items));
  }

  save(): void {
    this.form.markAllAsTouched();
    const v = this.form.value;

    if (!v.name?.trim()) { this.toastService.showError('Tenant name is required.'); return; }
    if (!v.contactNumber?.trim()) { this.toastService.showError('Mobile number is required.'); return; }
    if (v.aadharNumber && !/^\d{12}$/.test(v.aadharNumber)) {
      this.toastService.showError('Aadhaar number must be exactly 12 digits.'); return;
    }

    if (v.hasRoom && !v.roomId) { this.toastService.showError('Please select a room.'); return; }

    if (v.hasAdvance) {
      if (!v.advanceAmount || v.advanceAmount <= 0) { this.toastService.showError('Enter a valid advance amount.'); return; }
      if (!v.paymentModeCode) { this.toastService.showError('Select a payment mode for advance.'); return; }
    }

    if (v.hasPayment) {
      if (!v.initialPaymentAmount || v.initialPaymentAmount <= 0) { this.toastService.showError('Enter a valid payment amount.'); return; }
      if (!v.initialPaymentModeCode) { this.toastService.showError('Select a payment mode for initial payment.'); return; }
      if (!v.initialPaidFrom) { this.toastService.showError('Enter paid from date.'); return; }
      if (!v.initialPaidUpto) { this.toastService.showError('Enter paid upto date.'); return; }
    }

    this.submitTenant();
  }

  private submitTenant(): void {
    const v = this.form.value;

    const payload = {
      name: v.name,
      contactNumber: v.contactNumber,
      aadharNumber: v.aadharNumber,
      email: v.email || null,
      roomId: v.hasRoom ? (v.roomId || null) : null,
      fromDate: v.hasRoom ? v.fromDate : null,
      notes: v.notes,
      hasAdvance: v.hasAdvance,
      advanceAmount: v.hasAdvance ? v.advanceAmount : null,
      paymentModeCode: v.hasAdvance ? v.paymentModeCode : null
    };

    this.tenantService.createTenant(payload).subscribe({
      next: (res: any) => {
        if (v.hasPayment && res?.tenantId) {
          this.paymentService.createPayment({
            tenantId: res.tenantId,
            PaymentFrequencyCode: 'MONTHLY',
            paidFrom: v.initialPaidFrom,
            paidUpto: v.initialPaidUpto,
            amount: v.initialPaymentAmount,
            paymentModeCode: v.initialPaymentModeCode
          }).subscribe({
            next: () => {
              this.toastService.showSuccess('Tenant and initial payment created.');
              this.router.navigate(['/tenant-list']);
            },
            error: () => {
              this.toastService.showSuccess('Tenant created. Payment failed — add it manually.');
              this.router.navigate(['/tenant-list']);
            }
          });
        } else {
          this.toastService.showSuccess('Created Tenant Successfully.');
          this.router.navigate(['/tenant-list']);
        }
      },
      error: (err: { error: any }) => {
        this.error = err.error || 'Failed to save tenant';
        this.toastService.showError(this.error);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/tenant-list']);
  }

  viewExistingTenant() {
    this.router.navigate(['/tenants', this.existingTenant.tenantId]);
  }

  createStayForExisting() {
    const v = this.form.value;
    if (!v.roomId) {
      this.toastService.showError('Please check "Add Room" and select a room first.');
      return;
    }
    const payload = {
      tenantId: this.existingTenant.tenantId,
      roomId: v.roomId,
      fromDate: v.fromDate,
      advanceAmount: v.advanceAmount
    };
    this.tenantService.createStay(payload).subscribe({
      next: () => {
        this.toastService.showSuccess('Stay created successfully.');
        this.router.navigate(['/tenants', this.existingTenant.tenantId]);
      },
      error: (err) => {
        this.toastService.showError(err.error);
      }
    });
  }
}
