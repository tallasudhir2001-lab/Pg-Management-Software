import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, map, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { Room } from '../../rooms/models/room.model';
import { Roomservice } from '../../rooms/services/roomservice';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { BookingService } from '../booking-service';
import { PaymentService } from '../../payments/services/payment-service';
import { ToastService } from '../../../shared/toast/toast-service';

// Handles both ASP.NET Core ValidationProblemDetails ({ errors: { Field: ["msg"] } })
// and ExceptionMiddleWare format ({ message: "..." }) and plain strings.
function extractErrorMessage(err: any, fallback: string): string {
  const body = err?.error;
  if (body?.errors) {
    const messages = (Object.values(body.errors) as string[][]).flat();
    return messages[0] ?? fallback;
  }
  if (typeof body?.message === 'string') return body.message;
  if (typeof body === 'string' && body) return body;
  return fallback;
}

@Component({
  selector: 'app-add-booking',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './add-booking.html',
  styleUrls: ['../../tenant/styles/tenant-form.css', './add-booking.css'],
})
export class AddBooking implements OnInit {
  form!: FormGroup;
  rooms$!: Observable<Room[]>;
  paymentModes$!: Observable<any[]>;
  error = '';

  existingTenant: any = null;
  checkingAadhar = false;
  hasActiveBooking = false;

  constructor(
    private fb: FormBuilder,
    private roomService: Roomservice,
    private tenantService: Tenantservice,
    private bookingService: BookingService,
    private paymentService: PaymentService,
    private toastService: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      aadharNumber: ['', [Validators.required, Validators.pattern(/^\d{12}$/)]],
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      contactNumber: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
      roomId: ['', Validators.required],
      scheduledCheckInDate: [null, Validators.required],
      hasAdvance: [false],
      advanceAmount: [null],
      paymentModeCode: [null],
      notes: [''],
    });

    this.rooms$ = this.roomService
      .getRooms({ page: 1, pageSize: 100 })
      .pipe(map((res) => res.items));

    this.paymentModes$ = this.paymentService.getPaymentModes();

    // Aadhaar lookup — same pattern as add-tenant
    this.form.get('aadharNumber')?.valueChanges
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap((value) => {
          if (!value || value.length !== 12) {
            this.existingTenant = null;
            this.hasActiveBooking = false;
            return of(null);
          }

          this.checkingAadhar = true;
          return this.tenantService.findByAadhar(value).pipe(
            catchError(() => of(null))
          );
        })
      )
      .subscribe((res) => {
        this.checkingAadhar = false;

        if (res) {
          this.existingTenant = res;
          // Auto-populate tenant fields
          this.form.patchValue({
            name: res.name,
            email: res.email,
            contactNumber: res.contactNumber,
          });

          // Check if tenant has an active booking
          this.bookingService.checkActiveBooking(res.tenantId).subscribe({
            next: (result: { hasActiveBooking: boolean; }) => {
              this.hasActiveBooking = result.hasActiveBooking;
            },
            error: () => {
              this.hasActiveBooking = false;
            },
          });
        } else {
          this.existingTenant = null;
          this.hasActiveBooking = false;
          this.form.patchValue({ name: '', email: '', contactNumber: '' });
        }
      });
  }

  save(): void {
    this.form.markAllAsTouched();
    const formValue = this.form.value;

    // Validations
    if (!formValue.aadharNumber || !/^\d{12}$/.test(formValue.aadharNumber)) {
      this.toastService.showError('Aadhaar number must be exactly 12 digits.');
      return;
    }

    if (this.hasActiveBooking) {
      this.toastService.showError('This tenant already has an active booking.');
      return;
    }

    if (!formValue.name?.trim()) {
      this.toastService.showError('Tenant name is required.');
      return;
    }

    if (!formValue.email?.trim()) {
      this.toastService.showError('Email is required.');
      return;
    }

    const contactNumber = formValue.contactNumber?.trim() ?? '';
    if (!contactNumber) {
      this.toastService.showError('Mobile number is required.');
      return;
    }
    if (!/^[6-9]\d{9}$/.test(contactNumber)) {
      this.toastService.showError('Enter a valid 10-digit Indian mobile number.');
      return;
    }

    if (!formValue.roomId) {
      this.toastService.showError('Please select a room.');
      return;
    }

    if (!formValue.scheduledCheckInDate) {
      this.toastService.showError('Please select a check-in date.');
      return;
    }

    if (formValue.hasAdvance) {
      if (!formValue.advanceAmount || formValue.advanceAmount <= 0) {
        this.toastService.showError('Enter a valid advance amount.');
        return;
      }
      if (!formValue.paymentModeCode) {
        this.toastService.showError('Select a payment mode for advance.');
        return;
      }
    }

    const payload = {
      aadharNumber: formValue.aadharNumber,
      name: formValue.name,
      email: formValue.email,
      contactNumber: contactNumber,
      roomId: formValue.roomId,
      scheduledCheckInDate: formValue.scheduledCheckInDate,
      advanceAmount: formValue.hasAdvance ? formValue.advanceAmount : null,
      paymentModeCode: formValue.hasAdvance ? formValue.paymentModeCode : null,
      notes: formValue.notes || null,
    };

    this.bookingService.createBooking(payload).subscribe({
      next: () => {
        this.toastService.showSuccess('Booking created successfully.');
        this.router.navigate(['/bookings']);
      },
      error: (err: any) => {
        this.error = extractErrorMessage(err, 'Failed to create booking.');
        this.toastService.showError(this.error);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/bookings']);
  }
}