import { Component, OnInit } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Room } from '../../rooms/models/room.model';
import { FormBuilder,FormGroup,ReactiveFormsModule,Validators } from '@angular/forms';
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
  standalone :true,
  imports: [ReactiveFormsModule,CommonModule],
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
    private toastService:ToastService
  ) {}

  

  // 🔹 Rooms for dropdown
  rooms$!: Observable<Room[]>;
  paymentModes$!: Observable<any[]>;
  error= '';

  existingTenant: any = null;
  checkingAadhar = false;

  
  ngOnInit(): void {
    this.form = this.fb.group({
    name: ['', Validators.required],
    contactNumber: ['', Validators.required],
    aadharNumber: ['',
      [
        Validators.pattern(/^\d{12}$/)
      ]
    ],
    roomId: ['', Validators.required], 
    fromDate: [null],
    hasAdvance: [false],
    advanceAmount: [null],
    paymentModeCode: [null],
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

      return this.tenantService.findByAadhar(value)
        .pipe(catchError(() => of(null)));

    })
  )
  .subscribe(res => {

    this.checkingAadhar = false;

    if (res) {
      this.existingTenant = res;
    } else {
      this.existingTenant = null;
    }

  });

  }
  private loadAvailableRooms(): void {
    this.rooms$ = this.roomService.getRooms({
      page: 1,
      pageSize: 100,
    }).pipe(
      map(res => res.items)
    );
  }
  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.value;

    const payload = {
      name: formValue.name,
      contactNumber: formValue.contactNumber,
      aadharNumber: formValue.aadharNumber,
      roomId: formValue.roomId,
      fromDate: formValue.fromDate,
      notes: formValue.notes,

      hasAdvance: formValue.hasAdvance,
      advanceAmount: formValue.hasAdvance ? formValue.advanceAmount : null,
      paymentModeCode: formValue.hasAdvance ? formValue.paymentModeCode : null
    };
    if (payload.hasAdvance) {
      if (payload.advanceAmount && payload.advanceAmount <= 0) {
        this.toastService.showError('Enter valid advance amount');
        return;
      }

      if (!payload.paymentModeCode) {
        this.toastService.showError('Select payment mode');
        return;
      }
    }
    this.tenantService.createTenant(payload).subscribe({
    next: () => {
      this.toastService.showSuccess('Created Tenant Successfully.');
      // Navigate back to tenant list
      this.router.navigate(['/tenant-list']);
    },
    error: err => {
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

  const payload = {
    tenantId: this.existingTenant.tenantId,
    roomId: this.form.value.roomId,
    fromDate: this.form.value.fromDate,
    advanceAmount: this.form.value.advanceAmount
  };
  if (!this.form.value.roomId) {
    this.toastService.showError('Please select a room first.');
    return;
  }

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
