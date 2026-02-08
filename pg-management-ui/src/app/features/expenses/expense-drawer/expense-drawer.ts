import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExpensesService, CategoryFilterItem } from '../services/expenses-service';
import { PaymentService } from '../../payments/services/payment-service';
import { ToastService } from '../../../shared/toast/toast-service';
import { map, Observable } from 'rxjs';

@Component({
  selector: 'app-expense-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './expense-drawer.html',
  styleUrl: './expense-drawer.css'
})
export class ExpenseDrawer implements OnInit, OnChanges  {
  @Input() mode: 'add' | 'view' | 'edit' = 'add';
  @Input() expenseId?: string;

  @Output() saved = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();
  @Output() expenseAdded = new EventEmitter<void>();

  // Static cache for categories and payment modes (shared across all instances)
  private static categoriesCache: CategoryFilterItem[] | null = null;
  private static paymentModesCache: { code: string; label: string }[] | null = null;
  private static isLoadingCategories = false;
  private static isLoadingPaymentModes = false;

  categories: CategoryFilterItem[] = [];
  paymentModes: { code: string; label: string }[] = [];

  // Form model
  model = {
    categoryId: null as number | null,
    amount: null as number | null,
    expenseDate: '',
    description: '',
    paymentModeCode: '',
    referenceNo: '',
    isRecurring: false,
    recurringFrequency: null as string | null
  };

  isSubmitting = false;

  constructor(
    private expenseService: ExpensesService, 
    private paymentService: PaymentService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Load categories and payment modes (with caching)
    this.loadCategoriesWithCache();
    this.loadPaymentModesWithCache();
    
    // Handle initial load based on mode
    if (this.mode === 'add') {
      this.setDefaultDate();
    } else if (this.expenseId) {
      // For view/edit modes, load the expense data
      this.loadExpense(this.expenseId);
    }
  }
  
  ngOnChanges(changes: SimpleChanges): void {
    // Only handle changes after initial setup
    if (changes['expenseId'] && !changes['expenseId'].firstChange && this.expenseId) {
      this.loadExpense(this.expenseId);
    }
    
    if (changes['mode'] && !changes['mode'].firstChange) {
      if (this.mode === 'add') {
        this.resetForm();
        this.setDefaultDate();
      } else if (this.expenseId) {
        this.loadExpense(this.expenseId);
      }
    }
  }

  private setDefaultDate() {
    this.model.expenseDate = new Date().toISOString().split('T')[0];
  }

  private resetForm() {
    this.model = {
      categoryId: null,
      amount: null,
      expenseDate: '',
      description: '',
      paymentModeCode: '',
      referenceNo: '',
      isRecurring: false,
      recurringFrequency: null
    };
  }

  private loadCategoriesWithCache() {
    // If already cached, use it immediately
    if (ExpenseDrawer.categoriesCache) {
      this.categories = ExpenseDrawer.categoriesCache;
      return;
    }

    // If another instance is already loading, wait a bit and check again
    if (ExpenseDrawer.isLoadingCategories) {
      setTimeout(() => this.loadCategoriesWithCache(), 100);
      return;
    }

    // Load from backend
    ExpenseDrawer.isLoadingCategories = true;
    this.expenseService.getCategories().subscribe({
      next: (cats) => {
        ExpenseDrawer.categoriesCache = cats;
        this.categories = cats;
        ExpenseDrawer.isLoadingCategories = false;
      },
      error: (err) => {
        ExpenseDrawer.isLoadingCategories = false;
        console.error('Error loading categories:', err);
      }
    });
  }

  private loadPaymentModesWithCache() {
    // If already cached, use it immediately
    if (ExpenseDrawer.paymentModesCache) {
      this.paymentModes = ExpenseDrawer.paymentModesCache;
      return;
    }

    // If another instance is already loading, wait a bit and check again
    if (ExpenseDrawer.isLoadingPaymentModes) {
      setTimeout(() => this.loadPaymentModesWithCache(), 100);
      return;
    }

    // Load from backend
    ExpenseDrawer.isLoadingPaymentModes = true;
    this.paymentService.getPaymentModes().subscribe({
      next: (modes) => {
        const mappedModes = modes.map(m => ({
          code: m.code,
          label: m.description
        }));
        ExpenseDrawer.paymentModesCache = mappedModes;
        this.paymentModes = mappedModes;
        ExpenseDrawer.isLoadingPaymentModes = false;
      },
      error: (err) => {
        ExpenseDrawer.isLoadingPaymentModes = false;
        console.error('Error loading payment modes:', err);
      }
    });
  }

  submit(): void {
    if (this.mode === 'view') return;
    if (!this.isFormValid()) return;

    this.isSubmitting = true;

    const payload = {
      categoryId: this.model.categoryId!,
      amount: this.model.amount!,
      expenseDate: this.model.expenseDate,
      description: this.model.description,
      paymentModeCode: this.model.paymentModeCode,
      referenceNo: this.model.referenceNo || undefined,
      isRecurring: this.model.isRecurring,
      recurringFrequency: this.model.recurringFrequency || undefined
    };

    let request$: Observable<void>;

    if (this.mode === 'edit' && this.expenseId) {
      request$ = this.expenseService.updateExpense(this.expenseId, payload);
    } else {
      request$ = this.expenseService.createExpense(payload).pipe(
        map(() => void 0) // normalize Observable<string> → Observable<void>
      );
    }

    request$.subscribe({
      next: () => {
        this.saved.emit();
        this.close();
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  private isFormValid(): boolean {
    return !!(
      this.model.categoryId &&
      this.model.amount &&
      this.model.expenseDate &&
      this.model.paymentModeCode
    );
  }
  
  private loadExpense(id: string): void {
    this.expenseService.getExpenseById(id).subscribe({
      next: (exp) => {
        this.model.categoryId = exp.categoryId;
        this.model.amount = exp.amount;
        this.model.expenseDate = exp.expenseDate.split('T')[0];
        this.model.description = exp.description || '';
        this.model.paymentModeCode = exp.paymentModeCode;
        this.model.referenceNo = exp.referenceNo || '';
        this.model.isRecurring = exp.isRecurring;
        this.model.recurringFrequency = exp.recurringFrequency || null;
        
        // Force Angular to detect changes
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toastService.showError('Failed to load expense details');
        console.error('Error loading expense:', err);
      }
    });
  }
}
