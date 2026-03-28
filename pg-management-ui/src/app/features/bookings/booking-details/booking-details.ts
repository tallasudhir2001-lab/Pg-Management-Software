import { Component, Input, OnChanges, OnInit, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, map, tap } from 'rxjs';
import { BookingService } from '../booking-service';
import { BookingDetails as BookingDetailsModel } from '../models/booking.model';
import { Room } from '../../rooms/models/room.model';
import { Roomservice } from '../../rooms/services/roomservice';
import { ToastService } from '../../../shared/toast/toast-service';
import { AdvanceService } from '../../advances/services/advance-service';
import { Advance } from '../../advances/models/advance.model';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { SettleAdvanceModal } from '../../advances/settle-advance-modal/settle-advance-modal';
import { HasAccessDirective } from '../../../shared/directives/has-access.directive';

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
  selector: 'app-booking-details',
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule, FormsModule, RouterLink, SettleAdvanceModal, HasAccessDirective],
  templateUrl: './booking-details.html',
  styleUrls: ['./booking-details.css'],
})
export class BookingDetails implements OnInit, OnChanges {
  @Input() bookingId!: string;
  @Input() mode: 'view' | 'edit' = 'view';
  @Input() showHeader: boolean = true;

  booking$!: Observable<BookingDetailsModel>;
  rooms$!: Observable<Room[]>;

  form!: FormGroup;
  saving = false;
  loading = true;

  // Cancel / Terminate flow state
  showCancelConfirm = false;
  showTerminateConfirm = false;
  showSettleModal = false;
  advanceToSettle: Advance | null = null;
  cancellingBooking = false;
  terminatingBooking = false;
  currentBookingId = '';
  pendingCancelTenantId = '';
  pendingAction: 'cancel' | 'terminate' = 'cancel';

  // Check In flow state
  checkingIn = false;
  showCheckInModal = false;
  checkInDate = '';
  pendingCheckInBooking: BookingDetailsModel | null = null;

  constructor(
    private fb: FormBuilder,
    private bookingService: BookingService,
    private roomService: Roomservice,
    private advanceService: AdvanceService,
    private tenantService: Tenantservice,
    private route: ActivatedRoute,
    private router: Router,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (!this.bookingId) {
      const routeId = this.route.snapshot.paramMap.get('id');
      if (routeId) {
        this.bookingId = routeId;
        const urlSegments = this.route.snapshot.url.map(s => s.path);
        this.mode = urlSegments.includes('edit') ? 'edit' : 'view';
        this.loadBooking();
      }
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['bookingId'] && this.bookingId) || changes['mode']) {
      this.loadBooking();
    }
  }

  private loadBooking(): void {
    this.loading = true;
    this.booking$ = this.bookingService.getBookingById(this.bookingId).pipe(
      tap(booking => {
        this.currentBookingId = booking.bookingId;
        this.buildForm(booking);
        this.loading = false;
        this.cdr.detectChanges();
      })
    );

    if (this.mode === 'edit') {
      this.rooms$ = this.roomService
        .getRooms({ page: 1, pageSize: 100 })
        .pipe(map(res => res.items));
    }
  }

  private buildForm(booking: BookingDetailsModel): void {
    const isDisabled = this.mode === 'view';
    const checkInDate = booking.scheduledCheckInDate
      ? booking.scheduledCheckInDate.substring(0, 10)
      : '';

    this.form = this.fb.group({
      roomId:               [{ value: booking.roomId,        disabled: isDisabled }, Validators.required],
      scheduledCheckInDate: [{ value: checkInDate,           disabled: isDisabled }, Validators.required],
      advanceAmount:        [{ value: booking.advanceAmount, disabled: isDisabled }, [Validators.required, Validators.min(0)]],
      notes:                [{ value: booking.notes ?? '',   disabled: isDisabled }],
    });
  }

  get isEditMode(): boolean {
    return this.mode === 'edit';
  }

  enableEdit(): void {
    this.mode = 'edit';
    this.rooms$ = this.roomService
      .getRooms({ page: 1, pageSize: 100 })
      .pipe(map(res => res.items));
    this.form.enable();
    this.cdr.detectChanges();
  }

  save(booking: BookingDetailsModel): void {
    if (this.form.invalid) {
      this.toastService.showError('Please fill in all required fields correctly.');
      this.form.markAllAsTouched();
      return;
    }
    if (this.saving) return;
    this.saving = true;

    const payload = {
      roomId: this.form.value.roomId,
      scheduledCheckInDate: this.form.value.scheduledCheckInDate,
      advanceAmount: this.form.value.advanceAmount,
      notes: this.form.value.notes || null,
    };

    this.bookingService.updateBooking(booking.bookingId, payload).subscribe({
      next: () => {
        this.saving = false;
        this.toastService.showSuccess('Booking updated successfully.');
        this.router.navigate(['/bookings', booking.bookingId]);
      },
      error: (err: any) => {
        this.toastService.showError(extractErrorMessage(err, 'Failed to update booking.'));
        this.loadBooking();
        this.saving = false;
      },
    });
  }

  // ── Cancel / Terminate shared flow ─────────────────────

  requestCancel(booking: BookingDetailsModel): void {
    this.currentBookingId = booking.bookingId;
    this.pendingCancelTenantId = booking.tenantId;
    this.pendingAction = 'cancel';
    if (booking.advanceAmount > 0) {
      this.showCancelConfirm = true;
    } else {
      this.performAction();
    }
  }

  requestTerminate(booking: BookingDetailsModel): void {
    this.currentBookingId = booking.bookingId;
    this.pendingCancelTenantId = booking.tenantId;
    this.pendingAction = 'terminate';
    if (booking.advanceAmount > 0) {
      this.showTerminateConfirm = true;
    } else {
      this.performAction();
    }
  }

  openSettleBeforeAction(fromTerminate = false): void {
    this.showCancelConfirm = false;
    this.showTerminateConfirm = false;
    this.advanceService.getAdvances(this.pendingCancelTenantId).subscribe({
      next: advances => {
        const unsettled = advances.find(a => !a.isSettled) ?? null;
        if (unsettled) {
          this.advanceToSettle = unsettled;
          this.showSettleModal = true;
        } else {
          this.toastService.showError('No unsettled advance found. Please settle it from the tenant page.');
        }
      },
      error: () => {
        this.toastService.showError('Could not load advance details. Please try again.');
      },
    });
  }

  skipAndProceed(): void {
    this.showCancelConfirm = false;
    this.showTerminateConfirm = false;
    this.performAction();
  }

  onAdvanceSettled(): void {
    this.showSettleModal = false;
    this.advanceToSettle = null;
    this.performAction();
  }

  onSettleModalClosed(): void {
    this.showSettleModal = false;
    this.advanceToSettle = null;
  }

  private performAction(): void {
    if (this.pendingAction === 'cancel') {
      this.cancellingBooking = true;
      this.bookingService.cancelBooking(this.currentBookingId).subscribe({
        next: () => {
          this.cancellingBooking = false;
          this.toastService.showSuccess('Booking cancelled.');
          this.loadBooking();
        },
        error: (err: any) => {
          this.cancellingBooking = false;
          this.toastService.showError(extractErrorMessage(err, 'Failed to cancel booking.'));
        },
      });
    } else {
      this.terminatingBooking = true;
      this.bookingService.terminateBooking(this.currentBookingId).subscribe({
        next: () => {
          this.terminatingBooking = false;
          this.toastService.showSuccess('Booking terminated.');
          this.loadBooking();
        },
        error: (err: any) => {
          this.terminatingBooking = false;
          this.toastService.showError(extractErrorMessage(err, 'Failed to terminate booking.'));
        },
      });
    }
  }

  // ── Check In flow ──────────────────────────────────────

  checkIn(booking: BookingDetailsModel): void {
    this.pendingCheckInBooking = booking;
    this.checkInDate = booking.scheduledCheckInDate.substring(0, 10);
    this.showCheckInModal = true;
    this.cdr.detectChanges();
  }

  confirmCheckIn(): void {
    if (!this.pendingCheckInBooking || !this.checkInDate) return;
    this.checkingIn = true;
    this.showCheckInModal = false;
    const booking = this.pendingCheckInBooking;
    this.tenantService.createStay({
      tenantId: booking.tenantId,
      roomId: booking.roomId,
      fromDate: this.checkInDate,
    }).subscribe({
      next: () => {
        this.checkingIn = false;
        this.toastService.showSuccess('Tenant checked in successfully.');
        this.router.navigate(['/tenants', booking.tenantId]);
      },
      error: (err: any) => {
        this.checkingIn = false;
        this.toastService.showError(extractErrorMessage(err, 'Failed to check in tenant.'));
      },
    });
  }

  cancelCheckIn(): void {
    this.showCheckInModal = false;
    this.pendingCheckInBooking = null;
    this.checkInDate = '';
  }

  // ── Helpers ────────────────────────────────────────────

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':     return 'status-active';
      case 'cancelled':  return 'status-cancelled';
      case 'terminated': return 'status-terminated';
      default:           return '';
    }
  }

  formatCurrency(amount: number): string {
    return '₹' + amount.toLocaleString('en-IN');
  }
}
