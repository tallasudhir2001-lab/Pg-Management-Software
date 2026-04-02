import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { BookingListItem } from '../models/booking.model';
import { BookingService } from '../booking-service';
import { ToastService } from '../../../shared/toast/toast-service';
import { AdvanceService } from '../../advances/services/advance-service';
import { Advance } from '../../advances/models/advance.model';
import { SettleAdvanceModal } from '../../advances/settle-advance-modal/settle-advance-modal';
import { HasAccessDirective } from '../../../shared/directives/has-access.directive';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SettleAdvanceModal, HasAccessDirective],
  templateUrl: './booking-list.html',
  styleUrl: './booking-list.css',
})
export class BookingList implements OnInit {
  bookings$!: Observable<PagedResults<BookingListItem>>;

  // Pagination
  currentPage = 1;
  pageSizeOptions = [5, 10, 25, 50];
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  pages: number[] = [];

  // Search & Sort
  searchText = '';
  sortBy = '';
  sortDir = 'desc';

  // Filters
  showFilters = false;
  filterStatus = 'Active';
  filterRoomId = '';
  filterFromDate = '';
  filterToDate = '';

  // Cancel / Terminate flow state
  showCancelConfirm = false;
  showTerminateConfirm = false;
  showSettleModal = false;
  advanceToSettle: Advance | null = null;
  pendingCancelBookingId = '';
  pendingCancelTenantId = '';
  pendingCancelAdvanceAmount = 0;
  pendingAction: 'cancel' | 'terminate' = 'cancel';

  constructor(
    private bookingService: BookingService,
    private advanceService: AdvanceService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.bookings$ = this.bookingService.getBookings({
      page: this.currentPage,
      pageSize: this.pageSize,
      status: this.filterStatus || undefined,
      roomId: this.filterRoomId || undefined,
      fromDate: this.filterFromDate || undefined,
      toDate: this.filterToDate || undefined,
      sortBy: this.sortBy || undefined,
      sortDir: this.sortDir,
    });

    this.bookings$.subscribe((res) => {
      this.totalCount = res.totalCount;
      this.totalPages = Math.ceil(res.totalCount / this.pageSize);
      this.pages = Array.from({ length: this.totalPages }, (_, i) => i + 1);
    });
  }

  // Sorting
  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDir = 'asc';
    }
    this.currentPage = 1;
    this.loadBookings();
  }

  isSortedBy(column: string): boolean {
    return this.sortBy === column;
  }

  isSortAsc(column: string): boolean {
    return this.sortBy === column && this.sortDir === 'asc';
  }

  // Pagination
  get startItem(): number {
    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadBookings();
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize = newSize;
    this.currentPage = 1;
    this.loadBookings();
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadBookings();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadBookings();
    }
  }

  // Filters
  openFilters(): void {
    this.showFilters = true;
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.showFilters = false;
    this.loadBookings();
  }

  clearFilters(): void {
    this.filterStatus = '';
    this.filterRoomId = '';
    this.filterFromDate = '';
    this.filterToDate = '';
    this.currentPage = 1;
    this.showFilters = false;
    this.loadBookings();
  }

  get hasActiveFilters(): boolean {
    return !!(
      this.filterStatus ||
      this.filterRoomId ||
      this.filterFromDate ||
      this.filterToDate
    );
  }

  // Actions
  cancelBooking(booking: BookingListItem): void {
    if (booking.status !== 'Active') return;
    this.pendingAction = 'cancel';
    this.pendingCancelBookingId = booking.bookingId;
    this.pendingCancelTenantId = booking.tenantId;
    this.pendingCancelAdvanceAmount = booking.advanceAmount;

    if (booking.advanceAmount > 0) {
      this.showCancelConfirm = true;
    } else {
      this.performAction();
    }
  }

  terminateBooking(booking: BookingListItem): void {
    if (booking.status !== 'Active') return;
    this.pendingAction = 'terminate';
    this.pendingCancelBookingId = booking.bookingId;
    this.pendingCancelTenantId = booking.tenantId;
    this.pendingCancelAdvanceAmount = booking.advanceAmount;

    if (booking.advanceAmount > 0) {
      this.showTerminateConfirm = true;
    } else {
      this.performAction();
    }
  }

  openSettleBeforeAction(): void {
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
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastService.showError('Could not load advance details. Please try again.');
        this.cdr.detectChanges();
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
      this.bookingService.cancelBooking(this.pendingCancelBookingId).subscribe({
        next: () => {
          this.toastService.showSuccess('Booking cancelled successfully.');
          this.loadBookings();
        },
        error: (err: any) => {
          this.toastService.showError(err?.error?.message ?? err?.error ?? 'Failed to cancel booking.');
        },
      });
    } else {
      this.bookingService.terminateBooking(this.pendingCancelBookingId).subscribe({
        next: () => {
          this.toastService.showSuccess('Booking terminated successfully.');
          this.loadBookings();
        },
        error: (err: any) => {
          this.toastService.showError(err?.error?.message ?? err?.error ?? 'Failed to terminate booking.');
        },
      });
    }
  }

  // Formatters
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }

  formatCurrency(amount: number): string {
    return '₹' + amount.toLocaleString('en-IN');
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active':
        return 'status-active';
      case 'cancelled':
        return 'status-cancelled';
      case 'terminated':
        return 'status-terminated';
      default:
        return '';
    }
  }
}