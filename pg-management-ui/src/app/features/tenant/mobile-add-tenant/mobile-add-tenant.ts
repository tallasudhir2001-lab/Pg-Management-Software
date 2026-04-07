import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, map, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { Roomservice } from '../../rooms/services/roomservice';
import { Tenantservice } from '../services/tenantservice';
import { PaymentService } from '../../payments/services/payment-service';
import { ToastService } from '../../../shared/toast/toast-service';
import { Room } from '../../rooms/models/room.model';

@Component({
  selector: 'app-mobile-add-tenant',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './mobile-add-tenant.html',
  styleUrl: './mobile-add-tenant.css'
})
export class MobileAddTenant implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private roomService = inject(Roomservice);
  private tenantService = inject(Tenantservice);
  private paymentService = inject(PaymentService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);

  form!: FormGroup;
  rooms$!: Observable<Room[]>;
  paymentModes$!: Observable<any[]>;
  existingTenant: any = null;
  checkingAadhar = false;

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      contactNumber: ['', Validators.required],
      aadharNumber: ['', [Validators.pattern(/^\d{12}$/)]],
      email: ['', [Validators.required, Validators.email]],
      hasRoom: [false],
      roomId: [''],
      fromDate: [null],
      stayType: ['MONTHLY'],
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

    this.rooms$ = this.roomService.getRooms({ page: 1, pageSize: 100 }).pipe(map(r => r.items));
    this.paymentModes$ = this.paymentService.getPaymentModes();

    this.form.get('aadharNumber')?.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(value => {
        if (!value || value.length !== 12) { this.existingTenant = null; return of(null); }
        this.checkingAadhar = true;
        return this.tenantService.findByAadhar(value).pipe(catchError(() => of(null)));
      })
    ).subscribe(res => {
      this.checkingAadhar = false;
      this.existingTenant = res || null;
      this.cdr.detectChanges();
    });
  }

  save() {
    this.form.markAllAsTouched();
    const v = this.form.value;

    if (!v.name?.trim()) { this.toastService.showError('Tenant name is required.'); return; }
    if (!v.contactNumber?.trim()) { this.toastService.showError('Mobile number is required.'); return; }
    if (!v.email?.trim()) { this.toastService.showError('Email is required.'); return; }
    if (v.aadharNumber && !/^\d{12}$/.test(v.aadharNumber)) { this.toastService.showError('Aadhaar must be 12 digits.'); return; }
    if (v.hasRoom && !v.roomId) { this.toastService.showError('Please select a room.'); return; }
    if (v.hasAdvance) {
      if (!v.advanceAmount || v.advanceAmount <= 0) { this.toastService.showError('Enter a valid advance amount.'); return; }
      if (!v.paymentModeCode) { this.toastService.showError('Select advance payment mode.'); return; }
    }
    if (v.hasPayment) {
      if (!v.initialPaymentAmount || v.initialPaymentAmount <= 0) { this.toastService.showError('Enter a valid payment amount.'); return; }
      if (!v.initialPaymentModeCode) { this.toastService.showError('Select initial payment mode.'); return; }
      if (!v.initialPaidFrom) { this.toastService.showError('Enter paid from date.'); return; }
      if (!v.initialPaidUpto) { this.toastService.showError('Enter paid upto date.'); return; }
    }
    this.submitTenant();
  }

  private submitTenant() {
    const v = this.form.value;
    const payload = {
      name: v.name, contactNumber: v.contactNumber, aadharNumber: v.aadharNumber, email: v.email,
      roomId: v.hasRoom ? (v.roomId || null) : null,
      fromDate: v.hasRoom ? v.fromDate : null,
      stayType: v.hasRoom ? v.stayType : null,
      notes: v.notes,
      hasAdvance: v.hasAdvance,
      advanceAmount: v.hasAdvance ? v.advanceAmount : null,
      paymentModeCode: v.hasAdvance ? v.paymentModeCode : null
    };

    this.tenantService.createTenant(payload).subscribe({
      next: (res: any) => {
        if (v.hasPayment && res?.tenantId) {
          this.paymentService.createPayment({
            tenantId: res.tenantId, PaymentFrequencyCode: 'MONTHLY',
            paidFrom: v.initialPaidFrom, paidUpto: v.initialPaidUpto,
            amount: v.initialPaymentAmount, paymentModeCode: v.initialPaymentModeCode
          }).subscribe({
            next: () => { this.toastService.showSuccess('Tenant and payment created.'); this.router.navigate(['/tenant-list']); },
            error: () => { this.toastService.showSuccess('Tenant created. Payment failed — add manually.'); this.router.navigate(['/tenant-list']); }
          });
        } else {
          this.toastService.showSuccess('Tenant created successfully.');
          this.router.navigate(['/tenant-list']);
        }
      },
      error: (err) => { this.toastService.showError(err.error || 'Failed to save tenant'); }
    });
  }

  cancel() { this.router.navigate(['/tenant-list']); }

  viewExistingTenant() { this.router.navigate(['/tenants', this.existingTenant.tenantId]); }

  createStayForExisting() {
    const v = this.form.value;
    if (!v.roomId) { this.toastService.showError('Check "Add Room" and select a room first.'); return; }
    this.tenantService.createStay({
      tenantId: this.existingTenant.tenantId, roomId: v.roomId,
      fromDate: v.fromDate, advanceAmount: v.advanceAmount
    }).subscribe({
      next: () => { this.toastService.showSuccess('Stay created successfully.'); this.router.navigate(['/tenants', this.existingTenant.tenantId]); },
      error: (err) => { this.toastService.showError(err.error); }
    });
  }
}
