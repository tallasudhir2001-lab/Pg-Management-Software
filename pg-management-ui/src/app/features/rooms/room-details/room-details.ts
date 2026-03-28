import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { Roomservice } from '../services/roomservice';
import { Room } from '../models/room.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { RoomTenant } from '../models/room.tenant.model';
import { BookingService } from '../../bookings/booking-service';
import { BookingListItem } from '../../bookings/models/booking.model';
import { AdvanceService } from '../../advances/services/advance-service';
import { Advance } from '../../advances/models/advance.model';
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
  selector: 'app-room-details',
  standalone: true,
  imports: [CommonModule, FormsModule, SettleAdvanceModal, HasAccessDirective],
  templateUrl: './room-details.html',
  styleUrl: './room-details.css',
})
export class RoomDetails implements OnInit {
  room$!: Observable<Room>;
  tenants$!: Observable<RoomTenant[]>;
  bookings: BookingListItem[] = [];

  model!: Room;
  isSaving = false;
  error = '';
  roomId = '';

  // Cancel flow
  showCancelConfirm = false;
  showSettleModal = false;
  advanceToSettle: Advance | null = null;
  pendingCancelBookingId = '';
  pendingCancelTenantId = '';
  pendingCancelAdvanceAmount = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private roomService: Roomservice,
    private bookingService: BookingService,
    private advanceService: AdvanceService,
    private cdr: ChangeDetectorRef,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('id')!;
    this.room$ = this.roomService.getRoomById(this.roomId).pipe(
      tap(room => { this.model = { ...room }; })
    );
    this.tenants$ = this.roomService.getTenantsByRoom(this.roomId);
    this.loadBookings();
  }

  private loadBookings(): void {
    this.bookingService.getBookings({ page: 1, pageSize: 100, roomId: this.roomId, status: 'Active' })
      .subscribe({
        next: res => {
          this.bookings = res.items;
          this.cdr.detectChanges();
        },
        error: () => {}
      });
  }

  save(): void {
    this.isSaving = true;
    this.roomService.updateRoom(this.model.roomId, {
      roomNumber: this.model.roomNumber,
      capacity: this.model.capacity,
      rentAmount: this.model.rentAmount,
      isAc: this.model.isAc
    }).subscribe({
      next: () => {
        this.toastService.showSuccess('Changes Saved Successfully.');
        this.router.navigate(['/room-list']);
      },
      error: (err: { error: string }) => {
        this.isSaving = false;
        this.error = err.error || 'Update failed';
        this.toastService.showError(this.error);
      }
    });
  }

  delete(): void {
    if (!confirm('Are you sure you want to delete this room?')) return;
    this.roomService.deleteRoom(this.model.roomId).subscribe({
      next: () => this.router.navigate(['/room-list']),
      error: (err: { error: string }) => {
        this.error = err.error || 'Delete failed';
        this.toastService.showError(this.error);
        this.cdr.detectChanges();
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/room-list']);
  }

  // ── Tenant actions ──────────────────────────────────────
  onAction(action: 'view' | 'edit', tenantId: string): void {
    if (action === 'view') this.router.navigate(['/tenants', tenantId]);
    else this.router.navigate(['/tenants', tenantId, 'edit']);
  }

  // ── Booking actions ─────────────────────────────────────
  viewBooking(bookingId: string): void {
    this.router.navigate(['/bookings', bookingId]);
  }

  editBooking(bookingId: string): void {
    this.router.navigate(['/bookings', bookingId, 'edit']);
  }

  requestCancelBooking(booking: BookingListItem): void {
    this.pendingCancelBookingId = booking.bookingId;
    this.pendingCancelTenantId = booking.tenantId;
    this.pendingCancelAdvanceAmount = booking.advanceAmount;

    if (booking.advanceAmount > 0) {
      this.showCancelConfirm = true;
    } else {
      this.performCancel();
    }
  }

  openSettleBeforeCancel(): void {
    this.showCancelConfirm = false;
    this.advanceService.getAdvances(this.pendingCancelTenantId).subscribe({
      next: advances => {
        const unsettled = advances.find(a => !a.isSettled) ?? null;
        if (unsettled) {
          this.advanceToSettle = unsettled;
          this.showSettleModal = true;
        } else {
          this.toastService.showError('No unsettled advance found. Please settle it from the tenant page.');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastService.showError('Could not load advance details. Please try again.');
        this.cdr.detectChanges();
      }
    });
  }

  skipAndCancel(): void {
    this.showCancelConfirm = false;
    this.performCancel();
  }

  onAdvanceSettled(): void {
    this.showSettleModal = false;
    this.advanceToSettle = null;
    this.performCancel();
  }

  onSettleModalClosed(): void {
    this.showSettleModal = false;
    this.advanceToSettle = null;
  }

  private performCancel(): void {
    this.bookingService.cancelBooking(this.pendingCancelBookingId).subscribe({
      next: () => {
        this.toastService.showSuccess('Booking cancelled.');
        this.loadBookings();
      },
      error: (err: any) => {
        this.toastService.showError(extractErrorMessage(err, 'Failed to cancel booking.'));
      }
    });
  }

  // ── Formatters ──────────────────────────────────────────
  formatCurrency(amount: number): string {
    return '₹' + amount.toLocaleString('en-IN');
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':     return 'badge-active';
      case 'cancelled':  return 'badge-cancelled';
      case 'terminated': return 'badge-terminated';
      default:           return '';
    }
  }
}
