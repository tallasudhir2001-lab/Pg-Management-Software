import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { BookingService } from '../booking-service';
import { BookingDetails as BookingDetailsModel } from '../models/booking.model';
import { Room } from '../../rooms/models/room.model';
import { Roomservice } from '../../rooms/services/roomservice';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { AdvanceService } from '../../advances/services/advance-service';
import { Advance } from '../../advances/models/advance.model';
import { PaymentMode } from '../../payments/models/payment-mode.model';
import { PaymentService } from '../../payments/services/payment-service';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-mobile-booking-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mobile-booking-details.html',
  styleUrl: './mobile-booking-details.css'
})
export class MobileBookingDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private bookingService = inject(BookingService);
  private roomService = inject(Roomservice);
  private tenantService = inject(Tenantservice);
  private advanceService = inject(AdvanceService);
  private paymentService = inject(PaymentService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);

  booking: BookingDetailsModel | null = null;
  loading = true;
  error = '';

  // Actions
  checkingIn = false;
  cancelling = false;
  terminating = false;

  // Check-in sheet
  showCheckIn = false;
  checkInDate = '';
  checkInRoomId = '';
  checkInRooms: Room[] = [];

  // Advance settle sheet
  showSettle = false;
  settlingAdvance: Advance | null = null;
  settleDeductedAmount = 0;
  settlePaymentModeCode = '';
  settleLoading = false;
  paymentModes: PaymentMode[] = [];
  pendingAction: 'cancel' | 'terminate' | null = null;

  // Confirm sheet
  showAdvanceConfirm = false;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadBooking(id);
    this.paymentService.getPaymentModes().subscribe(m => {
      this.paymentModes = m;
      this.cdr.detectChanges();
    });
  }

  loadBooking(id: string) {
    this.loading = true;
    this.bookingService.getBookingById(id).subscribe({
      next: (b) => {
        this.booking = b;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load booking';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack() { this.router.navigate(['/bookings']); }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active': return 'status-active';
      case 'cancelled': return 'status-cancelled';
      case 'terminated': return 'status-terminated';
      default: return '';
    }
  }

  // Check In
  openCheckIn() {
    if (!this.booking) return;
    this.checkInDate = this.booking.scheduledCheckInDate.substring(0, 10);
    this.checkInRoomId = this.booking.roomId;
    this.roomService.getRooms({ page: 1, pageSize: 100 }).pipe(
      map(res => res.items)
    ).subscribe(rooms => {
      this.checkInRooms = rooms;
      this.showCheckIn = true;
      this.cdr.detectChanges();
    });
  }

  closeCheckIn() { this.showCheckIn = false; }

  confirmCheckIn() {
    if (!this.booking || !this.checkInDate || !this.checkInRoomId) return;
    this.checkingIn = true;
    this.showCheckIn = false;
    this.tenantService.createStay({
      tenantId: this.booking.tenantId,
      roomId: this.checkInRoomId,
      fromDate: this.checkInDate
    }).subscribe({
      next: () => {
        this.checkingIn = false;
        this.toastService.showSuccess('Tenant checked in successfully');
        this.router.navigate(['/tenants', this.booking!.tenantId]);
      },
      error: (err: any) => {
        this.checkingIn = false;
        this.toastService.showError(err?.error || 'Check-in failed');
        this.cdr.detectChanges();
      }
    });
  }

  // Cancel / Terminate
  requestAction(action: 'cancel' | 'terminate') {
    if (!this.booking) return;
    this.pendingAction = action;
    if (this.booking.advanceAmount > 0) {
      this.showAdvanceConfirm = true;
    } else {
      this.performAction();
    }
  }

  skipAndProceed() {
    this.showAdvanceConfirm = false;
    this.performAction();
  }

  openSettleBeforeAction() {
    if (!this.booking) return;
    this.showAdvanceConfirm = false;
    this.advanceService.getAdvances(this.booking.tenantId).subscribe({
      next: (advances) => {
        const unsettled = advances.find(a => !a.isSettled);
        if (unsettled) {
          this.settlingAdvance = unsettled;
          this.settleDeductedAmount = 0;
          this.settlePaymentModeCode = '';
          this.showSettle = true;
        } else {
          this.toastService.showError('No unsettled advance found');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastService.showError('Could not load advance');
        this.cdr.detectChanges();
      }
    });
  }

  closeSettle() { this.showSettle = false; this.settlingAdvance = null; }

  get refundAmount(): number {
    if (!this.settlingAdvance) return 0;
    return this.settlingAdvance.amount - this.settleDeductedAmount;
  }

  confirmSettle() {
    if (!this.settlingAdvance) return;
    if (this.settleDeductedAmount < 0) {
      this.toastService.showError('Deduction cannot be negative'); return;
    }
    if (this.settleDeductedAmount > this.settlingAdvance.amount) {
      this.toastService.showError('Deduction cannot exceed advance'); return;
    }
    if (!this.settlePaymentModeCode && this.refundAmount > 0) {
      this.toastService.showError('Select payment mode for refund'); return;
    }
    this.settleLoading = true;
    this.advanceService.settleAdvance(this.settlingAdvance.advanceId, {
      deductedAmount: this.settleDeductedAmount,
      paymentModeCode: this.settlePaymentModeCode
    }).subscribe({
      next: () => {
        this.settleLoading = false;
        this.toastService.showSuccess('Advance settled');
        this.showSettle = false;
        this.settlingAdvance = null;
        this.performAction();
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.settleLoading = false;
        this.toastService.showError(err?.error || 'Failed to settle');
        this.cdr.detectChanges();
      }
    });
  }

  private performAction() {
    if (!this.booking || !this.pendingAction) return;
    const id = this.booking.bookingId;

    if (this.pendingAction === 'cancel') {
      this.cancelling = true;
      this.bookingService.cancelBooking(id).subscribe({
        next: () => {
          this.cancelling = false;
          this.toastService.showSuccess('Booking cancelled');
          this.loadBooking(id);
        },
        error: (err: any) => {
          this.cancelling = false;
          this.toastService.showError(err?.error || 'Failed to cancel');
          this.cdr.detectChanges();
        }
      });
    } else {
      this.terminating = true;
      this.bookingService.terminateBooking(id).subscribe({
        next: () => {
          this.terminating = false;
          this.toastService.showSuccess('Booking terminated');
          this.loadBooking(id);
        },
        error: (err: any) => {
          this.terminating = false;
          this.toastService.showError(err?.error || 'Failed to terminate');
          this.cdr.detectChanges();
        }
      });
    }
    this.pendingAction = null;
  }
}
