import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExpensesService, ExpenseListItemDto } from '../services/expenses-service';

@Component({
  selector: 'app-mobile-expenses',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-expenses.html',
  styleUrl: './mobile-expenses.css'
})
export class MobileExpenses implements OnInit {
  private expensesService = inject(ExpensesService);
  private cdr = inject(ChangeDetectorRef);

  expenses: ExpenseListItemDto[] = [];
  loading = true;
  error = '';
  totalCount = 0;

  ngOnInit() {
    this.loadExpenses();
  }

  loadExpenses() {
    this.loading = true;
    this.error = '';

    this.expensesService.getExpenses({ page: 1, pageSize: 50 }).subscribe({
      next: (result) => {
        this.expenses = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load expenses';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }
}
