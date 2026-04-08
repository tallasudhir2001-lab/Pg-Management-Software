import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, map, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { Subject, Subscription } from 'rxjs';
import { Room } from '../../rooms/models/room.model';
import { Roomservice } from '../../rooms/services/roomservice';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { BookingService } from '../booking-service';
import { PaymentService } from '../../payments/services/payment-service';
import { PaymentMode } from '../../payments/models/payment-mode.model';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-mobile-add-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mobile-add-booking.html',
  styleUrl: './mobile-add-booking.css'
})
export class MobileAddBooking implements OnInit {
  private roomService = inject(Roomservice);
  private tenantService = inject(Tenantservice);
  private bookingService = inject(BookingService);
  private paymentService = inject(PaymentService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  rooms: Room[] = [];
  paymentModes: PaymentMode[] = [];

  // Form
  aadharNumber = '';
  name = '';
  email = '';
  contactNumber = '';
  roomId = '';
  scheduledCheckInDate = '';
  hasAdvance = false;
  advanceAmount: number | null = null;
  paymentModeCode = '';
  notes = '';

  // Aadhaar lookup
  existingTenant: any = null;
  checkingAadhar = false;
  hasActiveBooking = false;
  saving = false;

  private aadharSubject = new Subject<string>();
  private aadharSub?: Subscription;

  ngOnInit() {
    this.roomService.getRooms({ page: 1, pageSize: 100 }).pipe(
      map(res => res.items)
    ).subscribe(rooms => {
      this.rooms = rooms;
      this.cdr.detectChanges();
    });

    this.paymentService.getPaymentModes().subscribe(modes => {
      this.paymentModes = modes;
      this.cdr.detectChanges();
    });

    this.aadharSub = this.aadharSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(value => {
        if (!value || value.length !== 12) {
          this.existingTenant = null;
          this.hasActiveBooking = false;
          return of(null);
        }
        this.checkingAadhar = true;
        return this.tenantService.findByAadhar(value).pipe(catchError(() => of(null)));
      })
    ).subscribe(res => {
      this.checkingAadhar = false;
      if (res) {
        this.existingTenant = res;
        this.name = res.name;
        this.email = res.email;
        this.contactNumber = res.contactNumber;
        this.bookingService.checkActiveBooking(res.tenantId).subscribe({
          next: (r: { hasActiveBooking: boolean }) => {
            this.hasActiveBooking = r.hasActiveBooking;
            this.cdr.detectChanges();
          },
          error: () => { this.hasActiveBooking = false; this.cdr.detectChanges(); }
        });
      } else {
        this.existingTenant = null;
        this.hasActiveBooking = false;
        this.name = '';
        this.email = '';
        this.contactNumber = '';
      }
      this.cdr.detectChanges();
    });
  }

  onAadharChange() {
    this.aadharSubject.next(this.aadharNumber);
  }

  cancel() {
    this.router.navigate(['/bookings']);
  }

  save() {
    if (!this.aadharNumber || !/^\d{12}$/.test(this.aadharNumber)) {
      this.toastService.showError('Aadhaar must be 12 digits'); return;
    }
    if (this.hasActiveBooking) {
      this.toastService.showError('Tenant already has an active booking'); return;
    }
    if (!this.name?.trim()) {
      this.toastService.showError('Tenant name is required'); return;
    }
    if (!this.email?.trim()) {
      this.toastService.showError('Email is required'); return;
    }
    if (!this.contactNumber?.trim() || !/^[6-9]\d{9}$/.test(this.contactNumber)) {
      this.toastService.showError('Valid 10-digit mobile required'); return;
    }
    if (!this.roomId) {
      this.toastService.showError('Select a room'); return;
    }
    if (!this.scheduledCheckInDate) {
      this.toastService.showError('Select a check-in date'); return;
    }
    if (this.hasAdvance) {
      if (!this.advanceAmount || this.advanceAmount <= 0) {
        this.toastService.showError('Enter a valid advance amount'); return;
      }
      if (!this.paymentModeCode) {
        this.toastService.showError('Select payment mode for advance'); return;
      }
    }

    this.saving = true;
    this.bookingService.createBooking({
      aadharNumber: this.aadharNumber,
      name: this.name,
      email: this.email,
      contactNumber: this.contactNumber,
      roomId: this.roomId,
      scheduledCheckInDate: this.scheduledCheckInDate,
      advanceAmount: this.hasAdvance ? this.advanceAmount : null,
      paymentModeCode: this.hasAdvance ? this.paymentModeCode : null,
      notes: this.notes || null
    }).subscribe({
      next: () => {
        this.saving = false;
        this.toastService.showSuccess('Booking created successfully');
        this.router.navigate(['/bookings']);
      },
      error: (err: any) => {
        this.saving = false;
        const msg = err?.error?.message || err?.error || 'Failed to create booking';
        this.toastService.showError(typeof msg === 'string' ? msg : 'Failed to create booking');
        this.cdr.detectChanges();
      }
    });
  }
}
