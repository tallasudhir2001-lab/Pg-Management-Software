import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ExpensesService {
  private readonly expCategoryUrl = `${environment.apiBaseUrl}`;
  private readonly baseUrl = `${environment.apiBaseUrl}/expenses`;

  constructor(private http: HttpClient) {}

  getExpenses(query: ExpenseListQueryDto): Observable<PagedResults<ExpenseListItemDto>> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.fromDate) {
      params = params.set('fromDate', query.fromDate.toISOString());
    }

    if (query.toDate) {
      params = params.set('toDate', query.toDate.toISOString());
    }

    if (query.categoryId !== undefined) {
      params = params.set('categoryId', query.categoryId.toString());
    }

    if (query.minAmount !== undefined) {
      params = params.set('minAmount', query.minAmount.toString());
    }

    if (query.maxAmount !== undefined) {
      params = params.set('maxAmount', query.maxAmount.toString());
    }

    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }

    if (query.sortDir) {
      params = params.set('sortDir', query.sortDir);
    }

    return this.http.get<PagedResults<ExpenseListItemDto>>(this.baseUrl, { params });
  }

  getExpenseSummary(
    fromDate?: Date,
    toDate?: Date,
    categoryId?: number
  ): Observable<ExpenseSummaryDto> {
    let params = new HttpParams();

    if (fromDate) {
      params = params.set('fromDate', fromDate.toISOString().split('T')[0]);
    }

    if (toDate) {
      params = params.set('toDate', toDate.toISOString().split('T')[0]);
    }

    if (categoryId) {
    params = params.set('categoryId', categoryId.toString());
    }
    
    return this.http.get<ExpenseSummaryDto>(`${this.baseUrl}/summary`, { params });
  }

  createExpense(dto: CreateExpenseDto): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/create-expense`, dto);
  }

  updateExpense(id: string, dto: UpdateExpenseDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/update-expense/${id}`, dto);
  }

  deleteExpense(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/delete-expense/${id}`);
  }

  // Get categories for filter dropdown
 getCategories(): Observable<CategoryFilterItem[]> {
  return this.http.get<CategoryDto[]>(`${this.expCategoryUrl}/expense-categories`)
    .pipe(
      map((categories: any[]) => categories.map(cat => ({
        categoryId: cat.id,  // Maps backend 'id' to 'categoryId'
        name: cat.name
      })))
    );
}
getExpenseById(id: string): Observable<ExpenseDetailsDto> {
  return this.http.get<ExpenseDetailsDto>(`${this.baseUrl}/${id}`);
}

}

// DTOs
export interface ExpenseListQueryDto {
  page: number;
  pageSize: number;
  fromDate?: Date;
  toDate?: Date;
  categoryId?: number;
  minAmount?: number;
  maxAmount?: number;
  sortBy?: string;
  sortDir?: string;
}

export interface ExpenseListItemDto {
  id: string;
  expenseDate: Date;
  category: string;
  amount: number;
  paymentMode: string;
  paymentModeLabel: string;
  description: string;
}

export interface ExpenseSummaryDto {
  totalExpense: number;
  categoryBreakdown: ExpenseCategorySummaryDto[];
}

export interface ExpenseCategorySummaryDto {
  categoryId: number;
  category: string;
  totalAmount: number;
}

export interface CreateExpenseDto {
  categoryId: number;
  amount: number;
  expenseDate: string;
  description: string;
  paymentModeCode: string;
  referenceNo?: string;
  isRecurring: boolean;
  recurringFrequency?: string;
}

export interface UpdateExpenseDto {
  categoryId: number;
  amount: number;
  expenseDate: string;
  description: string;
  paymentModeCode: string;
  referenceNo?: string;
  isRecurring: boolean;
  recurringFrequency?: string;
}

export interface CategoryDto {
  categoryId: number;
  name: string;
}
export interface CategoryFilterItem {
  categoryId: number;
  name: string;
}
export interface ExpenseDetailsDto {
  id: string;
  categoryId: number;
  amount: number;
  expenseDate: string;        // YYYY-MM-DD
  description: string;
  paymentModeCode: string;
  referenceNo?: string;
  isRecurring: boolean;
  recurringFrequency?: string;
}
