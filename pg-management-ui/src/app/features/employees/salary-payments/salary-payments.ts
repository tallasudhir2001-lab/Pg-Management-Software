import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subject, combineLatest, map, startWith, switchMap, tap, of, catchError } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { EmployeeService } from '../services/employee-service';
import { SalaryPaymentListItem, CreateSalaryPaymentDto, UpdateSalaryPaymentDto, EmployeeListItem } from '../models/employee.model';
import { HasAccessDirective } from '../../../shared/directives/has-access.directive';

export const PAYMENT_MODES = [
  { code: 'CASH', label: 'Cash' },
  { code: 'UPI',  label: 'UPI'  },
  { code: 'BANK', label: 'Bank Transfer' },
];

@Component({
  selector: 'app-salary-payments',
  standalone: true,
  imports: [CommonModule, FormsModule, HasAccessDirective],
  templateUrl: './salary-payments.html',
  styleUrl: './salary-payments.css'
})
export class SalaryPayments implements OnInit {
  payments$!: Observable<PagedResults<SalaryPaymentListItem>>;
  private refresh$ = new Subject<void>();
  loading = true;

  readonly paymentModes = PAYMENT_MODES;

  // Pagination
  pageSizeOptions = [5, 10, 25, 50];
  pageSize = 10;
  currentPage = 1;
  totalPages = 0;
  pages: number[] = [];
  maxVisiblePages = 5;
  totalCount = 0;
  totalAmount = 0;

  // Filters
  selectedEmployeeId: string | null = null;
  selectedForMonth: string | null = null;
  selectedFromDate: string | null = null;
  selectedToDate: string | null = null;

  // Employee dropdown
  employees: EmployeeListItem[] = [];
  filteredEmployees: EmployeeListItem[] = [];
  employeeSearchText = '';
  isEmployeeDropdownOpen = false;
  selectedEmployeeLabel = '';

  // Sorting
  sortBy = 'paymentDate';
  sortDir: 'asc' | 'desc' = 'desc';

  // Drawer
  showDrawer = false;
  drawerMode: 'add' | 'edit' = 'add';
  editingPaymentId: string | null = null;
  showFilters = false;
  formEmployeeId = '';
  formAmount: number | null = null;
  formPaymentDate = '';
  formForMonth = '';
  formPaymentMode = 'CASH';
  formNotes = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadEmployeesForFilter();

    this.payments$ = combineLatest([
      this.route.queryParamMap,
      this.refresh$.pipe(startWith(undefined))
    ]).pipe(
      map(([params]) => {
        const page = Number(params.get('page')) || 1;
        const employeeId = params.get('employeeId');
        const forMonth = params.get('forMonth');
        const fromDate = params.get('fromDate');
        const toDate = params.get('toDate');
        const sortBy = params.get('sortBy') || 'paymentDate';
        const sortDir = (params.get('sortDir') as 'asc' | 'desc') || 'desc';

        this.currentPage = page;
        this.selectedEmployeeId = employeeId;
        this.selectedForMonth = forMonth;
        this.selectedFromDate = fromDate;
        this.selectedToDate = toDate;
        this.sortBy = sortBy;
        this.sortDir = sortDir;

        return { page, employeeId, forMonth, fromDate, toDate, sortBy, sortDir };
      }),
      switchMap(q =>
        this.employeeService.getSalaryPayments({
          page: q.page,
          pageSize: this.pageSize,
          employeeId: q.employeeId || undefined,
          forMonth: q.forMonth || undefined,
          fromDate: q.fromDate || undefined,
          toDate: q.toDate || undefined,
          sortBy: q.sortBy,
          sortDir: q.sortDir
        }).pipe(
          catchError(err => {
            console.error('Failed to load salary payments', err);
            this.loading = false;
            return of({ items: [], totalCount: 0, totalAmount: 0 } as PagedResults<SalaryPaymentListItem>);
          })
        )
      ),
      tap(result => {
        this.totalCount = result.totalCount;
        this.totalAmount = result.totalAmount ?? 0;
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.pages = this.getVisiblePages();
        this.loading = false;
      })
    );
  }

  // ===========================
  // Employee Filter Dropdown
  // ===========================
  private loadEmployeesForFilter(): void {
    this.employeeService.getEmployees({ page: 1, pageSize: 500, isActive: true }).subscribe({
      next: (result) => {
        this.employees = result.items;
        this.filteredEmployees = result.items;
      }
    });
  }

  onEmployeeSearch(): void {
    const search = this.employeeSearchText.toLowerCase();
    this.filteredEmployees = this.employees.filter(e =>
      e.name.toLowerCase().includes(search)
    );
  }

  selectEmployee(emp: EmployeeListItem): void {
    this.selectedEmployeeId = emp.employeeId;
    this.selectedEmployeeLabel = emp.name;
    this.isEmployeeDropdownOpen = false;
    this.applyFilters();
  }

  selectEmployeeInDrawer(emp: EmployeeListItem): void {
    this.selectedEmployeeId = emp.employeeId;
    this.selectedEmployeeLabel = emp.name;
    this.employeeSearchText = '';
    this.isEmployeeDropdownOpen = false;
  }

  clearEmployeeFilter(): void {
    this.selectedEmployeeId = null;
    this.selectedEmployeeLabel = '';
    this.employeeSearchText = '';
    this.applyFilters();
  }

  // ===========================
  // Filters & Sorting
  // ===========================
  applyFilters(): void {
    this.navigateWithParams({
      page: 1,
      employeeId: this.selectedEmployeeId,
      forMonth: this.selectedForMonth,
      fromDate: this.selectedFromDate,
      toDate: this.selectedToDate
    });
  }

  clearFilters(): void {
    this.selectedEmployeeId = null;
    this.selectedEmployeeLabel = '';
    this.employeeSearchText = '';
    this.selectedForMonth = null;
    this.selectedFromDate = null;
    this.selectedToDate = null;
    this.showFilters = false;
    this.navigateWithParams({
      page: 1,
      employeeId: null,
      forMonth: null,
      fromDate: null,
      toDate: null
    });
  }

  applyFiltersAndClose(): void {
    this.applyFilters();
    this.showFilters = false;
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDir = 'desc';
    }
    this.navigateWithParams({ sortBy: this.sortBy, sortDir: this.sortDir });
  }

  isSortedBy(col: string): boolean { return this.sortBy === col; }
  isSortAsc(col: string): boolean { return this.sortBy === col && this.sortDir === 'asc'; }

  // ===========================
  // Pagination
  // ===========================
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.navigateWithParams({ page });
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.navigateWithParams({ page: 1 });
  }

  getVisiblePages(): number[] {
    const half = Math.floor(this.maxVisiblePages / 2);
    let start = Math.max(1, this.currentPage - half);
    let end = start + this.maxVisiblePages - 1;
    if (end > this.totalPages) {
      end = this.totalPages;
      start = Math.max(1, end - this.maxVisiblePages + 1);
    }
    const pages: number[] = [];
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  // ===========================
  // Drawer
  // ===========================
  openAddDrawer(): void {
    this.drawerMode = 'add';
    this.editingPaymentId = null;
    this.formEmployeeId = '';
    this.formAmount = null;
    this.formPaymentDate = new Date().toISOString().split('T')[0];
    this.formForMonth = new Date().toISOString().slice(0, 7);
    this.formPaymentMode = 'CASH';
    this.formNotes = '';
    this.showDrawer = true;
  }

  openEditDrawer(payment: SalaryPaymentListItem): void {
    this.drawerMode = 'edit';
    this.editingPaymentId = payment.salaryPaymentId;
    this.formEmployeeId = payment.employeeId;
    this.formAmount = payment.amount;
    this.formPaymentDate = payment.paymentDate.split('T')[0];
    this.formForMonth = payment.forMonth;
    this.formPaymentMode = payment.paymentModeCode;
    this.formNotes = payment.notes || '';
    this.showDrawer = true;
  }

  closeDrawer(): void {
    this.showDrawer = false;
  }

  onCreatePayment(): void {
    if (!this.formEmployeeId || !this.formAmount || !this.formPaymentDate || !this.formForMonth) return;

    const dto: CreateSalaryPaymentDto = {
      employeeId: this.formEmployeeId,
      amount: this.formAmount,
      paymentDate: this.formPaymentDate,
      forMonth: this.formForMonth,
      paymentModeCode: this.formPaymentMode,
      notes: this.formNotes || undefined
    };

    this.employeeService.createSalaryPayment(dto).subscribe({
      next: () => {
        this.toastService.showSuccess('Salary payment recorded');
        this.closeDrawer();
        this.refresh$.next();
      },
      error: (err) => this.toastService.showError(err.error?.message || 'Failed to record payment')
    });
  }

  onUpdatePayment(): void {
    if (!this.editingPaymentId || !this.formAmount || !this.formPaymentDate || !this.formForMonth) return;

    const dto: UpdateSalaryPaymentDto = {
      amount: this.formAmount,
      paymentDate: this.formPaymentDate,
      forMonth: this.formForMonth,
      paymentModeCode: this.formPaymentMode,
      notes: this.formNotes || undefined
    };

    this.employeeService.updateSalaryPayment(this.editingPaymentId, dto).subscribe({
      next: () => {
        this.toastService.showSuccess('Salary payment updated');
        this.closeDrawer();
        this.refresh$.next();
      },
      error: (err) => this.toastService.showError(err.error?.message || 'Failed to update payment')
    });
  }

  onDeletePayment(payment: SalaryPaymentListItem): void {
    if (!confirm(`Delete salary payment for ${payment.employeeName} (${payment.forMonth})?`)) return;

    this.employeeService.deleteSalaryPayment(payment.salaryPaymentId).subscribe({
      next: () => {
        this.toastService.showSuccess('Salary payment deleted');
        this.refresh$.next();
      },
      error: (err) => this.toastService.showError(err.error?.message || 'Failed to delete payment')
    });
  }

  // ===========================
  // Helpers
  // ===========================
  formatCurrency(val?: number): string {
    if (val == null) return '—';
    return '₹' + val.toLocaleString('en-IN');
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatMonth(month: string): string {
    if (!month) return '—';
    const [y, m] = month.split('-');
    const date = new Date(Number(y), Number(m) - 1);
    return date.toLocaleDateString('en-IN', { month: 'short', year: 'numeric' });
  }

  formatPaymentMode(label: string): string {
    return label || '—';
  }

  private navigateWithParams(params: Record<string, any>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      queryParamsHandling: 'merge'
    });
  }
}
