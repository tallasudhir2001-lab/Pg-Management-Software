import { Component, ChangeDetectorRef, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subject, combineLatest, map, startWith, switchMap, tap } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { EmployeeService } from '../services/employee-service';
import { EmployeeListItem, EmployeeDetails, EmployeeRole, CreateEmployeeDto, UpdateEmployeeDto } from '../models/employee.model';
import { HasAccessDirective } from '../../../shared/directives/has-access.directive';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, FormsModule, HasAccessDirective],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.css'
})
export class EmployeeList implements OnInit {
  employees$!: Observable<PagedResults<EmployeeListItem>>;
  private refresh$ = new Subject<void>();
  loading = true;

  // Pagination
  pageSizeOptions = [5, 10, 25, 50];
  pageSize = 10;
  currentPage = 1;
  totalPages = 0;
  pages: number[] = [];
  maxVisiblePages = 5;
  totalCount = 0;

  // Filters
  searchText = '';
  filterActive: 'all' | 'active' | 'inactive' = 'all';

  // Sorting
  sortBy = 'name';
  sortDir: 'asc' | 'desc' = 'asc';

  // Drawer state
  showDrawer = false;
  drawerMode: 'add' | 'view' | 'edit' = 'add';
  selectedEmployee: EmployeeDetails | null = null;

  // Form fields
  formName = '';
  formContact = '';
  formRoleCode = '';
  formJoinDate = '';
  formSalary: number | null = null;
  formNotes = '';
  formIsActive = true;

  // Role options
  roles: EmployeeRole[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.employeeService.getRoles().subscribe(roles => this.roles = roles);

    this.employees$ = combineLatest([
      this.route.queryParamMap,
      this.refresh$.pipe(startWith(undefined))
    ]).pipe(
      map(([params]) => {
        const page = Number(params.get('page')) || 1;
        const search = params.get('search') || '';
        const active = params.get('isActive');
        const sortBy = params.get('sortBy') || 'name';
        const sortDir = (params.get('sortDir') as 'asc' | 'desc') || 'asc';

        this.currentPage = page;
        this.searchText = search;
        this.sortBy = sortBy;
        this.sortDir = sortDir;

        if (active === 'true') this.filterActive = 'active';
        else if (active === 'false') this.filterActive = 'inactive';
        else this.filterActive = 'all';

        return { page, search, active, sortBy, sortDir };
      }),
      switchMap(q =>
        this.employeeService.getEmployees({
          page: q.page,
          pageSize: this.pageSize,
          search: q.search || undefined,
          isActive: q.active === 'true' ? true : q.active === 'false' ? false : undefined,
          sortBy: q.sortBy,
          sortDir: q.sortDir
        })
      ),
      tap(result => {
        this.totalCount = result.totalCount;
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.pages = this.getVisiblePages();
        this.loading = false;
      })
    );
  }

  // ===========================
  // Search & Filters
  // ===========================
  private searchTimeout: any;

  onSearchChange(value: string): void {
    this.searchText = value;
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.navigateWithParams({ page: 1, search: value || null });
    }, 400);
  }

  onFilterActive(filter: 'all' | 'active' | 'inactive'): void {
    this.filterActive = filter;
    const isActive = filter === 'active' ? 'true' : filter === 'inactive' ? 'false' : null;
    this.navigateWithParams({ page: 1, isActive });
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDir = 'asc';
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
  // Drawer operations
  // ===========================
  openAddDrawer(): void {
    this.drawerMode = 'add';
    this.resetForm();
    this.showDrawer = true;
  }

  openViewDrawer(emp: EmployeeListItem): void {
    this.drawerMode = 'view';
    // Set partial data immediately so the drawer isn't blank
    this.selectedEmployee = {
      employeeId: emp.employeeId,
      name: emp.name,
      contactNumber: emp.contactNumber,
      roleCode: emp.roleCode,
      roleName: emp.roleName,
      joinDate: emp.joinDate,
      currentSalary: emp.currentSalary,
      isActive: emp.isActive,
      notes: undefined,
      createdAt: '',
      salaryHistory: []
    };
    this.showDrawer = true;
    // Load full details (salary history, notes, etc.)
    this.loadEmployeeDetails(emp.employeeId);
  }

  openEditDrawer(emp: EmployeeListItem): void {
    this.drawerMode = 'edit';
    this.showDrawer = true;
    this.loadEmployeeDetails(emp.employeeId);
  }

  closeDrawer(): void {
    this.showDrawer = false;
    this.selectedEmployee = null;
  }

  private loadEmployeeDetails(id: string): void {
    this.employeeService.getEmployeeById(id).subscribe({
      next: (emp) => {
        this.selectedEmployee = emp;
        if (this.drawerMode === 'edit') {
          this.formName = emp.name;
          this.formContact = emp.contactNumber || '';
          this.formRoleCode = emp.roleCode || '';
          this.formSalary = emp.currentSalary || null;
          this.formNotes = emp.notes || '';
          this.formIsActive = emp.isActive;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load employee details', err);
        this.toastService.showError('Failed to load employee details');
      }
    });
  }

  // ===========================
  // CRUD
  // ===========================
  onCreateEmployee(): void {
    if (!this.formName || !this.formJoinDate || !this.formSalary) return;

    const dto: CreateEmployeeDto = {
      name: this.formName,
      contactNumber: this.formContact || undefined,
      roleCode: this.formRoleCode || undefined,
      joinDate: this.formJoinDate,
      salary: this.formSalary,
      notes: this.formNotes || undefined
    };

    this.employeeService.createEmployee(dto).subscribe({
      next: () => {
        this.toastService.showSuccess('Employee created successfully');
        this.closeDrawer();
        this.refresh$.next();
      },
      error: (err) => this.toastService.showError(err.error?.message || 'Failed to create employee')
    });
  }

  onUpdateEmployee(): void {
    if (!this.selectedEmployee) return;

    const dto: UpdateEmployeeDto = {
      name: this.formName,
      contactNumber: this.formContact,
      roleCode: this.formRoleCode,
      salary: this.formSalary || undefined,
      isActive: this.formIsActive,
      notes: this.formNotes
    };

    this.employeeService.updateEmployee(this.selectedEmployee.employeeId, dto).subscribe({
      next: () => {
        this.toastService.showSuccess('Employee updated successfully');
        this.closeDrawer();
        this.refresh$.next();
      },
      error: (err) => this.toastService.showError(err.error?.message || 'Failed to update employee')
    });
  }

  onDeleteEmployee(emp: EmployeeListItem): void {
    if (!confirm(`Are you sure you want to delete ${emp.name}?`)) return;

    this.employeeService.deleteEmployee(emp.employeeId).subscribe({
      next: () => {
        this.toastService.showSuccess('Employee deleted');
        this.refresh$.next();
      },
      error: (err) => this.toastService.showError(err.error?.message || 'Failed to delete employee')
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

  private resetForm(): void {
    this.formName = '';
    this.formContact = '';
    this.formRoleCode = '';
    this.formJoinDate = new Date().toISOString().split('T')[0];
    this.formSalary = null;
    this.formNotes = '';
    this.formIsActive = true;
  }

  private navigateWithParams(params: Record<string, any>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      queryParamsHandling: 'merge'
    });
  }
}
