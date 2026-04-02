import { ChangeDetectorRef, Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HasAccessDirective } from '../../../shared/directives/has-access.directive';
import { PaymentHistoryDto } from '../models/paymets-history-dto';
import { PaymentType } from '../models/Payment type.model';
import { PaymentService } from '../services/payment-service';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PagedResults } from '../../../shared/models/page-results.model';
import {
  map,
  Observable,
  switchMap,
  tap,
  Subject,
  combineLatest,
  startWith,
} from 'rxjs';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { ToastService } from '../../../shared/toast/toast-service';
import { UserService } from '../../../shared/services/user-service';
import { ReceiptDrawer } from '../receipt-drawer/receipt-drawer';

export const PAYMENT_MODES: PaymentModeOption[] = [
  { code: 'cash', label: 'Cash' },
  { code: 'upi',  label: 'UPI'  },
  { code: 'card', label: 'Card' },
  { code: 'bank', label: 'Bank' },
];

@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, HasAccessDirective, ReceiptDrawer],
  templateUrl: './payments-history.html',
  styleUrl: './payments-history.css',
})
export class PaymentsHistory implements OnInit {
  payments$!: Observable<PagedResults<PaymentHistoryDto>>;
  private refresh$ = new Subject<void>();

  // Payment types — lazy-loaded on first drawer open
  paymentTypes: PaymentType[] = [];
  private paymentTypesLoaded = false;

  // Expose mode options to template
  readonly paymentModes = PAYMENT_MODES;

  // Pagination
  pageSizeOptions = [5, 10, 25, 50];
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

  users: UserFilterItem[] = [];
  filteredUsers: UserFilterItem[] = [];
  userSearchText = '';
  selectedUserId: string | null = null;
  isUserDropdownOpen = false;
  selectedUserLabel = '';

  // ✅ Multi-select mode filter (was single string filterPaymentMode)
  filterModes: string[] = [];

  // Payment type multi-select filter
  filterTypes: string[] = [];

  sortBy = 'paymentDate';
  sortDir: 'asc' | 'desc' = 'desc';
  totalCount = 0;

  // Receipt drawer
  activeReceiptPaymentId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentService: PaymentService,
    private tenantService: Tenantservice,
    private toastService: ToastService,
    private userService: UserService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTenantsForFilter();
    this.loadUsersForFilter();

    this.payments$ = combineLatest([
      this.route.queryParamMap,
      this.refresh$.pipe(startWith(undefined)),
    ]).pipe(
      map(([params]) => {
        const page    = Number(params.get('page')) || 1;
        const search  = params.get('search') || '';
        const tenantId= params.get('tenantId');
        const userId  = params.get('userId');
        const sortBy  = params.get('sortBy') || 'paymentDate';
        const sortDir = (params.get('sortDir') as 'asc' | 'desc') || 'desc';

        // ✅ modes: comma-separated in URL → string[]
        const modesParam = params.get('mode');
        const modes: string[] = modesParam
          ? modesParam.split(',').filter(m => m.length > 0)
          : [];

        const typesParam = params.get('types');
        const types: string[] = typesParam
          ? typesParam.split(',').filter(t => t.length > 0)
          : [];

        this.currentPage      = page;
        this.searchText       = search;
        this.filterModes      = modes;
        this.selectedTenantId = tenantId;
        this.selectedUserId   = userId;
        this.sortBy           = sortBy;
        this.sortDir          = sortDir;
        this.filterTypes      = types;

        return { page, search, modes, tenantId, userId, types, sortBy, sortDir };
      }),
      switchMap(q =>
        this.paymentService.getPaymentHistory({
          page:     q.page,
          pageSize: this.pageSize,
          search:   q.search,
          mode:     q.modes.length > 0 ? q.modes.join(',') : undefined,
          tenantId: q.tenantId ?? undefined,
          userId:   q.userId ?? undefined,
          types:    q.types.length > 0 ? q.types : undefined,
          sortBy:   q.sortBy,
          sortDir:  q.sortDir,
        })
      ),
      tap(result => {
        this.totalCount = result.totalCount;
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.buildPages();
      })
    );
  }

  // ─── Filter drawer ─────────────────────────────────────────────────────────

  openFilters(): void {
    this.showFilters = true;
    if (!this.paymentTypesLoaded) {
      this.paymentService.getPaymentTypes().subscribe(types => {
        this.paymentTypes       = types;
        this.paymentTypesLoaded = true;
        this.cdr.detectChanges(); // ✅ force view update — data arrives outside Angular's awareness
      });
    }
  }

  // ─── Search ────────────────────────────────────────────────────────────────

  onSearchChange(value: string): void {
    this.updateUrl({ search: value || null, page: 1 });
  }

  // ─── Pagination ────────────────────────────────────────────────────────────

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.updateUrl({ page });
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize = newSize;
    this.updateUrl({ page: 1 });
  }

  nextPage(): void { if (this.currentPage < this.totalPages) this.goToPage(this.currentPage + 1); }
  prevPage(): void { if (this.currentPage > 1) this.goToPage(this.currentPage - 1); }

  // ─── Filters ───────────────────────────────────────────────────────────────

  applyFilters(): void {
    this.updateUrl({
      page:     1,
      mode:     this.filterModes.length > 0 ? this.filterModes.join(',') : null,
      tenantId: this.selectedTenantId || null,
      userId:   this.selectedUserId || null,
      types:    this.filterTypes.length > 0 ? this.filterTypes.join(',') : null,
    });
    this.showFilters = false;
  }

  clearFilters(): void {
    this.filterModes         = [];
    this.filterTypes         = [];
    this.selectedTenantId    = null;
    this.selectedTenantLabel = '';
    this.tenantSearchText    = '';
    this.selectedUserId      = null;
    this.selectedUserLabel   = '';
    this.userSearchText      = '';
    this.updateUrl({ page: 1, mode: null, tenantId: null, userId: null, types: null, sortBy: null, sortDir: null });
    this.showFilters = false;
  }

  // ✅ Type checkbox toggle
  toggleType(code: string): void {
    this.filterTypes = this.filterTypes.includes(code)
      ? this.filterTypes.filter(t => t !== code)
      : [...this.filterTypes, code];
  }
  isTypeSelected(code: string): boolean { return this.filterTypes.includes(code); }

  // ✅ Mode checkbox toggle
  toggleMode(code: string): void {
    this.filterModes = this.filterModes.includes(code)
      ? this.filterModes.filter(m => m !== code)
      : [...this.filterModes, code];
  }
  isModeSelected(code: string): boolean { return this.filterModes.includes(code); }

  // Computed getter for clear-filter button visibility
  get hasActiveFilters(): boolean {
    return (
      this.filterModes.length > 0 || this.filterTypes.length > 0 ||
      !!this.selectedTenantId || !!this.selectedUserId ||
      this.sortBy !== 'paymentDate' || this.sortDir !== 'desc'
    );
  }

  // ─── URL helper ────────────────────────────────────────────────────────────

  private updateUrl(params: {
    page?:     number;
    search?:   string | null;
    mode?:     string | null;
    tenantId?: string | null;
    userId?:   string | null;
    types?:    string | null;
    sortBy?:   string | null;
    sortDir?:  string | null;
  }): void {
    const cleanParams: any = {};
    Object.keys(params).forEach(key => {
      const value = (params as any)[key];
      if (value === null)                       cleanParams[key] = null;
      else if (value !== undefined && value !== '') cleanParams[key] = value;
    });
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: cleanParams,
      queryParamsHandling: 'merge',
    });
  }

  // ─── Pagination helpers ────────────────────────────────────────────────────

  buildPages(): void {
    const half = Math.floor(this.maxVisiblePages / 2);
    let start  = this.currentPage - half;
    let end    = this.currentPage + half;
    if (start < 1) { start = 1; end = Math.min(this.maxVisiblePages, this.totalPages); }
    if (end > this.totalPages) { end = this.totalPages; start = Math.max(1, end - this.maxVisiblePages + 1); }
    this.pages = [];
    for (let i = start; i <= end; i++) this.pages.push(i);
  }

  // ─── Tenant filter ─────────────────────────────────────────────────────────

  private loadTenantsForFilter(): void {
    this.tenantService.getTenants({ page: 1, pageSize: 200, status: 'active' })
      .subscribe((res: { items: any[] }) => {
        this.tenants         = res.items.map(t => ({ tenantId: t.tenantId, name: t.name }));
        this.filteredTenants = [...this.tenants];
      });
  }

  onTenantInput(value: string): void {
    this.selectedTenantId = null; this.selectedTenantLabel = ''; this.tenantSearchText = value;
    this.filteredTenants  = this.tenants.filter(t => t.name.toLowerCase().includes(value.toLowerCase()));
    this.isTenantDropdownOpen = true;
  }
  selectTenant(tenant: TenantFilterItem): void {
    this.selectedTenantId = tenant.tenantId; this.selectedTenantLabel = tenant.name;
    this.tenantSearchText = ''; this.isTenantDropdownOpen = false;
  }
  onTenantKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Backspace' && this.selectedTenantId) {
      this.selectedTenantId = null; this.selectedTenantLabel = ''; this.tenantSearchText = '';
      this.filteredTenants  = [...this.tenants]; this.isTenantDropdownOpen = true;
      event.preventDefault();
    }
  }

  // ─── User filter ───────────────────────────────────────────────────────────

  private loadUsersForFilter(): void {
    this.userService.getUsers({ page: 1, pageSize: 200 })
      .subscribe((res: { items: { userId: any; name: any }[] }) => {
        this.users         = res.items.map(u => ({ userId: u.userId, name: u.name }));
        this.filteredUsers = [...this.users];
      });
  }

  onUserInput(value: string): void {
    this.selectedUserId = null; this.selectedUserLabel = ''; this.userSearchText = value;
    this.filteredUsers  = this.users.filter(u => u.name.toLowerCase().includes(value.toLowerCase()));
    this.isUserDropdownOpen = true;
  }
  selectUser(user: UserFilterItem): void {
    this.selectedUserId = user.userId; this.selectedUserLabel = user.name;
    this.userSearchText = ''; this.isUserDropdownOpen = false;
  }
  onUserKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Backspace' && this.selectedUserId) {
      this.selectedUserId = null; this.selectedUserLabel = ''; this.userSearchText = '';
      this.filteredUsers  = [...this.users]; this.isUserDropdownOpen = true;
      event.preventDefault();
    }
  }

  // ─── Outside click ─────────────────────────────────────────────────────────

  @HostListener('document:click', ['$event'])
  onOutsideClick(event: MouseEvent) {
    if (!(event.target as HTMLElement).closest('.searchable-dropdown')) {
      this.isTenantDropdownOpen = false;
      this.isUserDropdownOpen   = false;
    }
  }

  // ─── Sorting ───────────────────────────────────────────────────────────────

  onSort(column: string): void {
    const direction: 'asc' | 'desc' =
      this.sortBy === column ? (this.sortDir === 'asc' ? 'desc' : 'asc')
      : column === 'paymentDate' ? 'desc' : 'asc';
    this.updateUrl({ page: 1, sortBy: column, sortDir: direction });
  }
  isSortedBy(column: string): boolean { return this.sortBy === column; }
  isSortAsc(column: string): boolean  { return this.sortBy === column && this.sortDir === 'asc'; }

  // ─── Receipt drawer ─────────────────────────────────────────────────────────

  openReceipt(payment: PaymentHistoryDto): void {
    this.activeReceiptPaymentId = payment.paymentId;
  }

  closeReceipt(): void {
    this.activeReceiptPaymentId = null;
  }

  // ─── Row actions ───────────────────────────────────────────────────────────

  onAction(action: 'view' | 'edit' | 'delete', payment: PaymentHistoryDto): void {
    switch (action) {
      case 'view':   this.router.navigate(['/payments', payment.paymentId]); break;
      case 'edit':   this.router.navigate(['/payments', payment.paymentId, 'edit']); break;
      case 'delete': this.confirmDeletePayment(payment); break;
    }
  }

  private confirmDeletePayment(payment: PaymentHistoryDto): void {
    if (!confirm(`Are you sure you want to delete payment for "${payment.tenantName}"?`)) return;
    this.paymentService.deletePayment(payment.paymentId).subscribe({
      next: () => { this.toastService.showSuccess('Payment Deleted Successfully.'); this.refresh$.next(); },
      error: (err: { error: any }) => {
        this.toastService.showError(err?.error || 'Failed to delete payment. Please try again.');
      },
    });
  }

  // ─── Pagination display ────────────────────────────────────────────────────

  get startItem(): number { return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1; }
  get endItem(): number   { return Math.min(this.currentPage * this.pageSize, this.totalCount); }

  // ─── Formatters ────────────────────────────────────────────────────────────

  formatCurrency(amount: number): string { return '₹' + amount.toLocaleString('en-IN'); }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatPaymentMode(mode: string): string {
    const map: Record<string, string> = { cash: 'Cash', upi: 'UPI', card: 'Card', bank: 'Bank' };
    return map[mode.toLowerCase()] || mode;
  }
}

// ─── Interfaces ──────────────────────────────────────────────────────────────

interface TenantFilterItem  { tenantId: string; name: string; }
interface UserFilterItem    { userId:   string; name: string; }
interface PaymentModeOption { code: string; label: string; }