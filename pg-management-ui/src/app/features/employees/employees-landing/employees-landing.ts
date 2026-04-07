import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmployeeList } from '../employee-list/employee-list';
import { SalaryPayments } from '../salary-payments/salary-payments';

@Component({
  selector: 'app-employees-landing',
  standalone: true,
  imports: [CommonModule, EmployeeList, SalaryPayments],
  templateUrl: './employees-landing.html',
  styleUrl: './employees-landing.css'
})
export class EmployeesLanding {
  activeTab: 'employees' | 'salaries' = 'employees';

  switchTab(tab: 'employees' | 'salaries'): void {
    this.activeTab = tab;
  }
}
