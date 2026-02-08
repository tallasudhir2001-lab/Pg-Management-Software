import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentHistoryDto } from '../models/paymets-history-dto';
import { PaymentService } from '../services/payment-service';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PagedResults } from '../../../shared/models/page-results.model';
import { distinctUntilChanged, map, Observable, switchMap, tap, Subject, combineLatest,startWith } from 'rxjs';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { ToastService } from '../../../shared/toast/toast-service';
import { UserService } from '../../../shared/services/user-service';

@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './payments-history.html',
  styleUrl: './payments-history.css',
})
export class PaymentsHistory implements OnInit {
  payments$!: Observable<PagedResults<PaymentHistoryDto>>;
  private refresh$ = new Subject<void>();

  // Pagination
  pageSize = 10;
  currentPage = 1;
  totalPages = 0;
  pages: number[] = [];
  maxVisiblePages = 5;

  searchText = '';

  showFilters = false;
  tenants: TenantFilterItem[] = [];
  filteredTenants: TenantFilterItem[] = [];

  tenantSearchText = '';
  selectedTenantId: string | null = null;
  isTenantDropdownOpen = false;
  selectedTenantLabel = '';
  
  // Collected By filter
  users: UserFilterItem[] = [];
  filteredUsers: UserFilterItem[] = [];
  userSearchText = '';
  selectedUserId: string | null = null;
  isUserDropdownOpen = false;
  selectedUserLabel = '';
  
  // Payment mode filter
  filterPaymentMode: '' | 'cash' | 'upi' | 'card' | 'bank' = '';

  // Sorting
  sortBy = 'paymentDate';
  sortDir: 'asc' | 'desc' = 'desc';

  // Pagination count
  totalCount = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentService: PaymentService,
    private tenantService: Tenantservice,
    private toastService: ToastService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    // Load tenants for filter dropdown
    this.loadTenantsForFilter();
    
    // Load users for collected by filter
    this.loadUsersForFilter();

    this.payments$ = combineLatest([
  this.route.queryParamMap,
  this.refresh$.pipe(startWith(undefined))
]).pipe(
  map(([params]) => {
    const page = Number(params.get('page')) || 1;
    const search = params.get('search') || '';
    const paymentMode = params.get('mode') || '';
    const tenantId = params.get('tenantId');
    const userId = params.get('userId');
    const sortBy = params.get('sortBy') || 'paymentDate';
    const sortDir = (params.get('sortDir') as 'asc' | 'desc') || 'desc';

    this.currentPage = page;
    this.searchText = search;
    this.filterPaymentMode = paymentMode as any;
    this.selectedTenantId = tenantId;
    this.selectedUserId = userId;
    this.sortBy = sortBy;
    this.sortDir = sortDir;

    return { page, search, paymentMode, tenantId, userId, sortBy, sortDir };
  }),
  switchMap(q =>
    this.paymentService.getPaymentHistory({
      page: q.page,
      pageSize: this.pageSize,
      search: q.search,
      mode: q.paymentMode,
      tenantId: q.tenantId ?? undefined,
      userId: q.userId ?? undefined,
      sortBy: q.sortBy,
      sortDir: q.sortDir
    })
  ),
  tap(result => {
    this.totalCount = result.totalCount;
    this.totalPages = Math.ceil(result.totalCount / this.pageSize);
    this.buildPages();
  })
);

  }

  // Search
  onSearchChange(value: string): void {
    this.updateUrl({ search: value ? value : null, page: 1 });
  }

  // Pagination helper methods
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.updateUrl({ page });
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  applyFilters(): void {
    this.updateUrl({
      page: 1,
      mode: this.filterPaymentMode || null,
      tenantId: this.selectedTenantId || null,
      userId: this.selectedUserId || null
    });

    this.showFilters = false;
  }

  clearFilters(): void {
    this.filterPaymentMode = '';
    this.selectedTenantId = null;
    this.selectedTenantLabel = '';
    this.tenantSearchText = '';
    this.selectedUserId = null;
    this.selectedUserLabel = '';
    this.userSearchText = '';

    this.updateUrl({ 
      page: 1, 
      mode: null, 
      tenantId: null,
      userId: null,
      sortBy: null, 
      sortDir: null 
    });
    this.showFilters = false;
  }

  private updateUrl(params: {
    page?: number;
    search?: string | null;
    mode?: string | null;
    tenantId?: string | null;
    userId?: string | null;
    sortBy?: string | null;
    sortDir?: string | null;
  }): void {
    const cleanParams: any = {};

    Object.keys(params).forEach(key => {
      const value = (params as any)[key];

      if (value === null) {
        cleanParams[key] = null;
      } else if (value !== undefined && value !== '') {
        cleanParams[key] = value;
      }
    });

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: cleanParams,
      queryParamsHandling: 'merge'
    });
  }

  buildPages(): void {
    const half = Math.floor(this.maxVisiblePages / 2);

    let start = this.currentPage - half;
    let end = this.currentPage + half;

    if (start < 1) {
      start = 1;
      end = Math.min(this.maxVisiblePages, this.totalPages);
    }

    if (end > this.totalPages) {
      end = this.totalPages;
      start = Math.max(1, end - this.maxVisiblePages + 1);
    }

    this.pages = [];
    for (let i = start; i <= end; i++) {
      this.pages.push(i);
    }
  }

  private loadTenantsForFilter(): void {
    this.tenantService.getTenants({
      page: 1,
      pageSize: 200,
      status: 'active' // Only active tenants
    }).subscribe((res: { items: any[]; }) => {
      this.tenants = res.items.map(t => ({
        tenantId: t.tenantId,
        name: t.name
      }));

      this.filteredTenants = [...this.tenants];
    });
  }

  private loadUsersForFilter(): void {
    this.userService.getUsers({
      page: 1,
      pageSize: 200 // Enough for dropdown
    }).subscribe((res: { items: { userId: any; name: any; }[]; }) => {
      this.users = res.items.map((u: { userId: any; name: any; }) => ({
        userId: u.userId,
        name: u.name
      }));

      this.filteredUsers = [...this.users];
    });
  }

  onTenantInput(value: string): void {
    this.selectedTenantId = null;
    this.selectedTenantLabel = '';
    this.tenantSearchText = value;

    const search = value.toLowerCase();
    this.filteredTenants = this.tenants.filter(t =>
      t.name.toLowerCase().includes(search)
    );

    this.isTenantDropdownOpen = true;
  }

  selectTenant(tenant: { tenantId: string; name: string }): void {
    this.selectedTenantId = tenant.tenantId;
    this.selectedTenantLabel = tenant.name;
    this.tenantSearchText = '';
    this.isTenantDropdownOpen = false;
  }

  onUserInput(value: string): void {
    this.selectedUserId = null;
    this.selectedUserLabel = '';
    this.userSearchText = value;

    const search = value.toLowerCase();
    this.filteredUsers = this.users.filter(u =>
      u.name.toLowerCase().includes(search)
    );

    this.isUserDropdownOpen = true;
  }

  selectUser(user: { userId: string; name: string }): void {
    this.selectedUserId = user.userId;
    this.selectedUserLabel = user.name;
    this.userSearchText = '';
    this.isUserDropdownOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onOutsideClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target.closest('.searchable-dropdown')) {
      this.isTenantDropdownOpen = false;
      this.isUserDropdownOpen = false;
    }
  }

  onSort(column: string): void {
    let direction: 'asc' | 'desc' = 'asc';

    if (this.sortBy === column) {
      direction = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      direction = column === 'paymentDate' ? 'desc' : 'asc';
    }

    this.updateUrl({
      page: 1,
      sortBy: column,
      sortDir: direction
    });
  }

  // Sort icon helper methods
  isSortedBy(column: string): boolean {
    return this.sortBy === column;
  }

  isSortAsc(column: string): boolean {
    return this.sortBy === column && this.sortDir === 'asc';
  }

  // Single action dispatcher method
  onAction(action: 'view' | 'edit' | 'delete', payment: PaymentHistoryDto): void {
    switch (action) {
      case 'view':
        this.viewPayment(payment.paymentId);
        break;

      case 'edit':
        this.editPayment(payment.paymentId);
        break;

      case 'delete':
        this.confirmDeletePayment(payment);
        break;
    }
  }

  // Payment action helpers
  private viewPayment(paymentId: string): void {
    this.router.navigate(['/payments', paymentId]);
  }

  private editPayment(paymentId: string): void {
    this.router.navigate(['/payments', paymentId, 'edit']);
  }

  private confirmDeletePayment(payment: PaymentHistoryDto): void {
    const confirmed = confirm(
      `Are you sure you want to delete payment for "${payment.tenantName}"?`
    );

    if (!confirmed) return;

    this.deletePayment(payment.paymentId);
  }

  private deletePayment(paymentId: string): void {
    this.paymentService.deletePayment(paymentId).subscribe({
      next: () => {
        this.toastService.showSuccess('Payment Deleted Successfully.');
        this.refresh$.next();
      },
      error: (err: { error: any; }) => {
        this.toastService.showError(err?.error || 'Failed to delete payment. Please try again.');
      }
    });
  }

  // Pagination helpers
  get startItem(): number {
    return this.totalCount === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(
      this.currentPage * this.pageSize,
      this.totalCount
    );
  }

  // Format currency
  formatCurrency(amount: number): string {
    return '₹' + amount.toLocaleString('en-IN');
  }

  // Format date
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  // Format payment mode
  formatPaymentMode(mode: string): string {
    const modeMap: { [key: string]: string } = {
      'cash': 'Cash',
      'upi': 'UPI',
      'card': 'Card',
      'bank': 'Bank'
    };
    return modeMap[mode.toLowerCase()] || mode;
  }
  onTenantKeyDown(event: KeyboardEvent): void {
  if (
    event.key === 'Backspace' &&
    this.selectedTenantId
  ) {
    // Clear selection
    this.selectedTenantId = null;
    this.selectedTenantLabel = '';
    this.tenantSearchText = '';
    this.filteredTenants = [...this.tenants];
    this.isTenantDropdownOpen = true;

    // Prevent browser doing anything weird
    event.preventDefault();
  }
}
  onUserKeyDown(event: KeyboardEvent): void {
  if (
    event.key === 'Backspace' &&
    this.selectedUserId
  ) {
    this.selectedUserId = null;
    this.selectedUserLabel = '';
    this.userSearchText = '';
    this.filteredUsers = [...this.users];
    this.isUserDropdownOpen = true;

    event.preventDefault();
  }
}

}

// Interfaces
interface TenantFilterItem {
  tenantId: string;
  name: string;
}

interface UserFilterItem {
  userId: string;
  name: string;
}
