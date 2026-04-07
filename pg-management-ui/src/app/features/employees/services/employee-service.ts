import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { environment } from '../../../../environments/environment';
import {
  EmployeeRole,
  EmployeeListItem,
  EmployeeDetails,
  CreateEmployeeDto,
  UpdateEmployeeDto,
  SalaryPaymentListItem,
  CreateSalaryPaymentDto,
  UpdateSalaryPaymentDto,
  EmployeeListQuery,
  SalaryPaymentListQuery
} from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly baseUrl = `${environment.apiBaseUrl}/employees`;

  constructor(private http: HttpClient) {}

  getRoles(): Observable<EmployeeRole[]> {
    return this.http.get<EmployeeRole[]>(`${this.baseUrl}/roles`);
  }

  getEmployees(query: EmployeeListQuery): Observable<PagedResults<EmployeeListItem>> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.search) params = params.set('search', query.search);
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive.toString());
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDir) params = params.set('sortDir', query.sortDir);

    return this.http.get<PagedResults<EmployeeListItem>>(this.baseUrl, { params });
  }

  getEmployeeById(id: string): Observable<EmployeeDetails> {
    return this.http.get<EmployeeDetails>(`${this.baseUrl}/${id}`);
  }

  createEmployee(dto: CreateEmployeeDto): Observable<any> {
    return this.http.post(this.baseUrl, dto);
  }

  updateEmployee(id: string, dto: UpdateEmployeeDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, dto);
  }

  deleteEmployee(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  // Salary Payments
  getSalaryPayments(query: SalaryPaymentListQuery): Observable<PagedResults<SalaryPaymentListItem>> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.employeeId) params = params.set('employeeId', query.employeeId);
    if (query.forMonth) params = params.set('forMonth', query.forMonth);
    if (query.fromDate) params = params.set('fromDate', query.fromDate);
    if (query.toDate) params = params.set('toDate', query.toDate);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDir) params = params.set('sortDir', query.sortDir);

    return this.http.get<PagedResults<SalaryPaymentListItem>>(`${this.baseUrl}/salary-payments`, { params });
  }

  createSalaryPayment(dto: CreateSalaryPaymentDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/salary-payments`, dto);
  }

  updateSalaryPayment(id: string, dto: UpdateSalaryPaymentDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/salary-payments/${id}`, dto);
  }

  deleteSalaryPayment(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/salary-payments/${id}`);
  }
}
