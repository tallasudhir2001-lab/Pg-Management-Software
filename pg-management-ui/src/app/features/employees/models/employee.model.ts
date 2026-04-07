export interface EmployeeRole {
  code: string;
  name: string;
}

export interface EmployeeListItem {
  employeeId: string;
  name: string;
  contactNumber?: string;
  roleCode?: string;
  roleName?: string;
  joinDate: string;
  currentSalary?: number;
  isActive: boolean;
}

export interface EmployeeDetails {
  employeeId: string;
  name: string;
  contactNumber?: string;
  roleCode?: string;
  roleName?: string;
  joinDate: string;
  currentSalary?: number;
  isActive: boolean;
  notes?: string;
  createdAt: string;
  salaryHistory: SalaryHistoryItem[];
}

export interface SalaryHistoryItem {
  id: string;
  amount: number;
  effectiveFrom: string;
  effectiveTo?: string;
}

export interface CreateEmployeeDto {
  name: string;
  contactNumber?: string;
  roleCode?: string;
  joinDate: string;
  salary: number;
  notes?: string;
}

export interface UpdateEmployeeDto {
  name?: string;
  contactNumber?: string;
  roleCode?: string;
  salary?: number;
  isActive?: boolean;
  notes?: string;
}

export interface SalaryPaymentListItem {
  salaryPaymentId: string;
  employeeId: string;
  employeeName: string;
  amount: number;
  paymentDate: string;
  forMonth: string;
  paymentModeCode: string;
  paymentModeLabel: string;
  notes?: string;
  paidBy?: string;
}

export interface CreateSalaryPaymentDto {
  employeeId: string;
  amount: number;
  paymentDate: string;
  forMonth: string;
  paymentModeCode: string;
  notes?: string;
}

export interface UpdateSalaryPaymentDto {
  amount: number;
  paymentDate: string;
  forMonth: string;
  paymentModeCode: string;
  notes?: string;
}

export interface EmployeeListQuery {
  page: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDir?: string;
}

export interface SalaryPaymentListQuery {
  page: number;
  pageSize: number;
  employeeId?: string;
  forMonth?: string;
  fromDate?: string;
  toDate?: string;
  sortBy?: string;
  sortDir?: string;
}
