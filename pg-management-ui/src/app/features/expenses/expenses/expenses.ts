import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, Subject, combineLatest, map, startWith, switchMap, tap } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { ExpenseListItemDto, ExpensesService, ExpenseSummaryDto,CategoryFilterItem } from '../services/expenses-service';
import { TimeHelper } from '../../../shared/utils/time.helper';
import { ExpenseDrawer } from '../expense-drawer/expense-drawer';

@Component({
  selector: 'app-expenses',
  standalone:true,
  imports: [CommonModule, FormsModule,ExpenseDrawer],
  templateUrl: './expenses.html',
  styleUrl: './expenses.css',
})
export class Expenses implements OnInit {
  expenses$!: Observable<PagedResults<ExpenseListItemDto>>;
  summary$!: Observable<ExpenseSummaryDto>;
  private refresh$ = new Subject<void>();

  // Pagination
  pageSize = 10;
  currentPage = 1;
  totalPages = 0;
  pages: number[] = [];
  maxVisiblePages = 5;
  totalCount = 0;

  // Filters
  showFilters = false;
  selectedFromDate: string | null = null;
  selectedToDate: string | null = null;
  selectedCategoryId: number | null = null;
  minAmount: number | null = null;
  maxAmount: number | null = null;

  // Categories for dropdown
  categories: CategoryFilterItem[] = [];
  filteredCategories: CategoryFilterItem[] = [];
  categorySearchText = '';
  isCategoryDropdownOpen = false;
  selectedCategoryLabel = '';

  // Sorting
  sortBy = 'expenseDate';
  sortDir: 'asc' | 'desc' = 'desc';

  showAddExpense = false;
  activeRange: 'all' | 'today' | 'week' | 'month' | 'custom' = 'all';

  showDrawer = false;
  drawerMode: 'add' | 'view' | 'edit' = 'add';
  selectedExpenseId?: string;


  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private expenseService: ExpensesService, // You'll need to create this service
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    // Load categories for filter dropdown
    this.loadCategoriesForFilter();

    // Get expenses list
    this.expenses$ = combineLatest([
      this.route.queryParamMap,
      this.refresh$.pipe(startWith(undefined))
    ]).pipe(
      map(([params]) => {
        const page = Number(params.get('page')) || 1;
        const fromDate = params.get('fromDate');
        const toDate = params.get('toDate');
        const categoryId = params.get('categoryId');
        const minAmount = params.get('minAmount');
        const maxAmount = params.get('maxAmount');
        const sortBy = params.get('sortBy') || 'expenseDate';
        const sortDir = (params.get('sortDir') as 'asc' | 'desc') || 'desc';

        this.currentPage = page;
        this.selectedFromDate = fromDate;
        this.selectedToDate = toDate;
        this.selectedCategoryId = categoryId ? Number(categoryId) : null;
        this.minAmount = minAmount ? Number(minAmount) : null;
        this.maxAmount = maxAmount ? Number(maxAmount) : null;
        this.sortBy = sortBy;
        this.sortDir = sortDir;

        return { page, fromDate, toDate, categoryId, minAmount, maxAmount, sortBy, sortDir };
      }),
      switchMap(q =>
        this.expenseService.getExpenses({
          page: q.page,
          pageSize: this.pageSize,
          fromDate: q.fromDate ? new Date(q.fromDate) : undefined,
          toDate: q.toDate ? new Date(q.toDate) : undefined,
          categoryId: q.categoryId ? Number(q.categoryId) : undefined,
          minAmount: q.minAmount ? Number(q.minAmount) : undefined,
          maxAmount: q.maxAmount ? Number(q.maxAmount) : undefined,
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

    // Get summary data
    this.summary$ = combineLatest([
      this.route.queryParamMap,
      this.refresh$.pipe(startWith(undefined))
    ]).pipe(
      map(([params]) => ({
        fromDate: params.get('fromDate'),
        toDate: params.get('toDate'),
        categoryId: params.get('categoryId')
      })),
      switchMap(q =>
        this.expenseService.getExpenseSummary(
          q.fromDate ? new Date(q.fromDate) : undefined,
          q.toDate ? new Date(q.toDate) : undefined,
          q.categoryId ? Number(q.categoryId) : undefined
        )
      )
    );
  }

  // Filter methods
  applyFilters(): void {
    this.updateUrl({
      page: 1,
      fromDate: this.selectedFromDate || null,
      toDate: this.selectedToDate || null,
      categoryId: this.selectedCategoryId?.toString() || null,
      minAmount: this.minAmount?.toString() || null,
      maxAmount: this.maxAmount?.toString() || null
    });
    this.showFilters = false;
  }

  clearFilters(): void {
    this.selectedFromDate = null;
    this.selectedToDate = null;
    this.selectedCategoryId = null;
    this.selectedCategoryLabel = '';
    this.categorySearchText = '';
    this.minAmount = null;
    this.maxAmount = null;
    this.activeRange = 'all';
    
    this.updateUrl({
      page: 1,
      fromDate: null,
      toDate: null,
      categoryId: null,
      minAmount: null,
      maxAmount: null,
      sortBy: null,
      sortDir: null
    });
    this.showFilters = false;
  }

  private updateUrl(params: {
    page?: number;
    fromDate?: string | null;
    toDate?: string | null;
    categoryId?: string | null;
    minAmount?: string | null;
    maxAmount?: string | null;
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

  // Pagination
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

  // Sorting
  onSort(column: string): void {
    let direction: 'asc' | 'desc' = 'asc';

    if (this.sortBy === column) {
      direction = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      direction = column === 'expenseDate' ? 'desc' : 'asc';
    }

    this.updateUrl({
      page: 1,
      sortBy: column,
      sortDir: direction
    });
  }

  isSortedBy(column: string): boolean {
    return this.sortBy === column;
  }

  isSortAsc(column: string): boolean {
    return this.sortBy === column && this.sortDir === 'asc';
  }

  // Category dropdown
  private loadCategoriesForFilter(): void {
    this.expenseService.getCategories().subscribe((categories) => {
      this.categories = categories.map(c => ({
        categoryId: c.categoryId,
        name: c.name
      }));
      this.filteredCategories = [...this.categories];
    });
  }

  onCategoryInput(value: string): void {
    this.selectedCategoryId = null;
    this.selectedCategoryLabel = '';
    this.categorySearchText = value;

    const search = value.toLowerCase();
    this.filteredCategories = this.categories.filter(c =>
      c.name.toLowerCase().includes(search)
    );

    this.isCategoryDropdownOpen = true;
  }

  selectCategory(category: { categoryId: number; name: string }): void {
    this.selectedCategoryId = category.categoryId;
    this.selectedCategoryLabel = category.name;
    this.categorySearchText = '';
    this.isCategoryDropdownOpen = false;
  }

  onCategoryKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Backspace' && this.selectedCategoryId) {
      this.selectedCategoryId = null;
      this.selectedCategoryLabel = '';
      this.categorySearchText = '';
      this.filteredCategories = [...this.categories];
      this.isCategoryDropdownOpen = true;
      event.preventDefault();
    }
  }

  @HostListener('document:click', ['$event'])
  onOutsideClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target.closest('.searchable-dropdown')) {
      this.isCategoryDropdownOpen = false;
    }
  }

  // Actions
 onAction(action: 'view' | 'edit' | 'delete', expense: ExpenseListItemDto): void {
  switch (action) {
    case 'view':
      this.openViewExpense(expense.id);
      break;

    case 'edit':
      this.openEditExpense(expense.id);
      break;

    case 'delete':
      this.confirmDeleteExpense(expense);
      break;
  }
}


  private viewExpense(expenseId: string): void {
    this.router.navigate(['/expenses', expenseId]);
  }

  private editExpense(expenseId: string): void {
    this.router.navigate(['/expenses', expenseId, 'edit']);
  }

  private confirmDeleteExpense(expense: ExpenseListItemDto): void {
    const confirmed = confirm(
      `Are you sure you want to delete this expense: "${expense.description}"?`
    );

    if (!confirmed) return;
    this.deleteExpense(expense.id);
  }

  private deleteExpense(expenseId: string): void {
    this.expenseService.deleteExpense(expenseId).subscribe({
      next: () => {
        this.toastService.showSuccess('Expense Deleted Successfully.');
        this.refresh$.next();
      },
      error: (err) => {
        this.toastService.showError(err?.error || 'Failed to delete expense. Please try again.');
      }
    });
  }

  // Pagination helpers
  get startItem(): number {
    return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  // Format helpers
  formatCurrency(amount: number): string {
    return '₹' + amount.toLocaleString('en-IN');
  }

  formatDate(date: string | Date): string {
    return new Date(date).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  formatPaymentMode(mode: string): string {
    return mode;
  }

  // Date selection helpers
  setDateRange(range: 'today' | 'week' | 'month' | 'custom'): void {
  this.activeRange = range; // Track which button was clicked
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  switch (range) {
    case 'today':
      this.selectedFromDate = TimeHelper.ToLocalDateString(today);
      this.selectedToDate = TimeHelper.ToLocalDateString(today);
      break;
    case 'week':
      const weekStart = new Date(today);
      weekStart.setDate(today.getDate() - today.getDay());
      this.selectedFromDate = TimeHelper.ToLocalDateString(weekStart);
      this.selectedToDate = TimeHelper.ToLocalDateString(today);
      break;
    case 'month':
      const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
      this.selectedFromDate = TimeHelper.ToLocalDateString(monthStart);
      this.selectedToDate = TimeHelper.ToLocalDateString(today);
      break;
  }
}
  openAddExpense() {
  this.showAddExpense = true;
  this.drawerMode = 'add';
  this.selectedExpenseId = undefined;
  this.showDrawer = true;
}

closeAddExpense() {
  this.showAddExpense = false;
}

onExpenseAdded() {
  this.showAddExpense = false;
  this.refresh$.next();
}
openViewExpense(id: string): void {
  this.drawerMode = 'view';
  this.selectedExpenseId = id;
  this.showDrawer = true;
}

openEditExpense(id: string): void {
  this.drawerMode = 'edit';
  this.selectedExpenseId = id;
  this.showDrawer = true;
}
closeDrawer(): void {
  this.showDrawer = false;
  this.selectedExpenseId = undefined;
}

onExpenseSaved(): void {
  this.showDrawer = false;
  this.selectedExpenseId = undefined;
  this.refresh$.next(); // reload grid + summary
}
// Add this method to your Expenses class
isRangeActive(range: 'today' | 'week' | 'month'): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const todayStr = TimeHelper.ToLocalDateString(today);

    switch (range) {
        case 'today':
            return this.selectedFromDate === todayStr && this.selectedToDate === todayStr;
        case 'week':
            const weekStart = new Date(today);
            weekStart.setDate(today.getDate() - today.getDay());
            return this.selectedFromDate === TimeHelper.ToLocalDateString(weekStart) && this.selectedToDate === todayStr;
        case 'month':
            const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
            return this.selectedFromDate === TimeHelper.ToLocalDateString(monthStart) && this.selectedToDate === todayStr;
        default:
            return false;
    }
}

}


