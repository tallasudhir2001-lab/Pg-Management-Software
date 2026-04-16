import { Routes } from '@angular/router';
import { Login } from './core/auth/login/login';
import { PgSelect } from './core/auth/pg-select/pg-select';
import { DashboardPage } from './features/dashboard/dashboard-page';
import { authGuard } from './core/guards/auth-guard';
import { permissionGuard } from './core/guards/permission-guard';
import { Layout } from './shared/layout/layout';
import { AdminLayout } from './admin/admin-layout/admin-layout';
import { adminGuard } from './admin/guards/admin-guard';
import { RegisterPg } from './admin/components/register-pg/register-pg';
import { RoomListPage } from './features/rooms/room-list-page';
import { AddRoomPage } from './features/rooms/add-room-page';
import { TenantListPage } from './features/tenant/tenant-list-page';
import { AddTenantPage } from './features/tenant/add-tenant-page';
import { RoomDetailsPage } from './features/rooms/room-details-page';
import { TenantDetailsPage } from './features/tenant/tenant-details-page';
import { AddPaymentTenantPage } from './features/payments/add-payment-tenant-page';
import { AddPaymentPage } from './features/payments/add-payment-page';
import { PaymentsLandingPage } from './features/payments/payments-landing-page';
import { ExpensesPage } from './features/expenses/expenses-page';
import { PaymentDetailsPage } from './features/payments/payment-details-page';
import { BookingListPage } from './features/bookings/booking-list-page';
import { AddBookingPage } from './features/bookings/add-booking-page';
import { BookingDetailsPage } from './features/bookings/booking-details-page';
import { RoleAccessComponent } from './admin/components/role-access/role-access';
import { AdminPgList } from './admin/components/pg-list/pg-list';
import { PgUserManagement } from './features/pg-users/pg-users';
import { ReportsLanding } from './features/reports/reports-landing/reports-landing';
import { RentCollectionReport } from './features/reports/rent-collection/rent-collection';
import { OverdueRentReport } from './features/reports/overdue-rent/overdue-rent';
import { PaymentHistoryReport } from './features/reports/payment-history-report/payment-history-report';
import { OccupancyReport } from './features/reports/occupancy/occupancy';
import { TenantListReport } from './features/reports/tenant-list-report/tenant-list-report';
import { AdvanceBalanceReport } from './features/reports/advance-balance/advance-balance';
import { ExpenseReport } from './features/reports/expense-report/expense-report';
import { ProfitLossReport } from './features/reports/profit-loss/profit-loss';
import { TenantTurnoverReport } from './features/reports/tenant-turnover/tenant-turnover';
import { RoomRevenueReport } from './features/reports/room-revenue/room-revenue';
import { SalaryReport } from './features/reports/salary-report/salary-report';
import { CashFlowReport } from './features/reports/cash-flow/cash-flow';
import { TenantAgingReport } from './features/reports/tenant-aging/tenant-aging';
import { RoomChangeHistoryReport } from './features/reports/room-change-history/room-change-history';
import { BookingConversionReport } from './features/reports/booking-conversion/booking-conversion';
import { Settings } from './features/settings/settings/settings';
import { ConfigurationsLanding } from './features/configurations/configurations-landing/configurations-landing';
import { ReportSubscriptions } from './features/settings/report-subscriptions/report-subscriptions';
import { AuditLog } from './features/audit/audit-log/audit-log';
import { EmployeesLanding } from './features/employees/employees-landing/employees-landing';

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'select-pg', component: PgSelect },
    {
        path: '',
        component: Layout,
        canActivate: [authGuard],
        children: [
            { path: 'dashboard', component: DashboardPage },
            { path: 'room-list', component: RoomListPage, canActivate: [permissionGuard], data: { requiredPermission: 'Room.GetRooms' } },
            { path: 'add-room', component: AddRoomPage, canActivate: [permissionGuard], data: { requiredPermission: 'Room.CreateRoom' } },
            { path: 'room/:id', component: RoomDetailsPage, canActivate: [permissionGuard], data: { requiredPermission: 'Room.GetRoomById' } },
            { path: 'tenant-list', component: TenantListPage, canActivate: [permissionGuard], data: { requiredPermission: 'Tenant.GetTenants' } },
            { path: 'tenants/add', component: AddTenantPage, canActivate: [permissionGuard], data: { requiredPermission: 'Tenant.CreateTenant' } },
            { path: 'tenants/:id', component: TenantDetailsPage, canActivate: [permissionGuard], data: { mode: 'view', requiredPermission: 'Tenant.GetTenantById' } },
            { path: 'tenants/:id/edit', component: TenantDetailsPage, canActivate: [permissionGuard], data: { mode: 'edit', requiredPermission: 'Tenant.UpdateTenant' } },
            {
                path: 'payments',
                children: [
                    { path: '', redirectTo: 'add', pathMatch: 'full' },
                    { path: 'add', component: AddPaymentPage, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.CreatePayment' } },
                    { path: 'add/:tenantId', component: AddPaymentTenantPage, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.CreatePayment' } },
                    { path: 'history', component: PaymentsLandingPage, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.GetPaymentHistory' } },
                    { path: ':paymentId', component: PaymentDetailsPage, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.GetPayment' } },
                    { path: ':paymentId/edit', component: PaymentDetailsPage, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.UpdatePayment' } }
                ]
            },
            { path: 'bookings', component: BookingListPage, canActivate: [permissionGuard], data: { requiredPermission: 'Booking.GetBookings' } },
            { path: 'bookings/add', component: AddBookingPage, canActivate: [permissionGuard], data: { requiredPermission: 'Booking.CreateBooking' } },
            { path: 'bookings/:id', component: BookingDetailsPage, canActivate: [permissionGuard], data: { mode: 'view', requiredPermission: 'Booking.GetById' } },
            { path: 'bookings/:id/edit', component: BookingDetailsPage, canActivate: [permissionGuard], data: { mode: 'edit', requiredPermission: 'Booking.UpdateBooking' } },
            { path: 'employees', component: EmployeesLanding, canActivate: [permissionGuard], data: { requiredPermission: 'Employee.GetEmployees' } },
            { path: 'expenses', component: ExpensesPage, canActivate: [permissionGuard], data: { requiredPermission: 'Expense.GetExpenses' } },
            { path: 'reports', component: ReportsLanding },
            { path: 'reports/rent-collection', component: RentCollectionReport },
            { path: 'reports/overdue-rent', component: OverdueRentReport },
            { path: 'reports/payment-history', component: PaymentHistoryReport },
            { path: 'reports/occupancy', component: OccupancyReport },
            { path: 'reports/tenant-list', component: TenantListReport },
            { path: 'reports/advance-balance', component: AdvanceBalanceReport },
            { path: 'reports/expenses', component: ExpenseReport },
            { path: 'reports/profit-loss', component: ProfitLossReport },
            { path: 'reports/tenant-turnover', component: TenantTurnoverReport },
            { path: 'reports/room-revenue', component: RoomRevenueReport },
            { path: 'reports/salary', component: SalaryReport },
            { path: 'reports/cash-flow', component: CashFlowReport },
            { path: 'reports/tenant-aging', component: TenantAgingReport },
            { path: 'reports/room-change-history', component: RoomChangeHistoryReport },
            { path: 'reports/booking-conversion', component: BookingConversionReport },
            { path: 'settings', component: ConfigurationsLanding },
            { path: 'settings/manage-users', component: PgUserManagement, canActivate: [permissionGuard], data: { requiredPermission: 'PgUser.GetUsers' } },
            { path: 'settings/notifications', component: Settings, canActivate: [permissionGuard], data: { requiredPermission: 'Settings.GetNotificationSettings' } },
            { path: 'settings/report-subscriptions', component: ReportSubscriptions, canActivate: [permissionGuard], data: { requiredPermission: 'Settings.GetReportSubscriptions' } },
            { path: 'audit-log', component: AuditLog, canActivate: [permissionGuard], data: { requiredPermission: 'Audit.GetAuditEvents' } }

        ]
    },
    {
        path: 'admin',
        component: AdminLayout,
        canActivate: [authGuard, adminGuard],
        children: [
            { path: '', redirectTo: 'register-pg', pathMatch: 'full' },
            { path: 'register-pg', component: RegisterPg },
            { path: 'pgs', component: AdminPgList },
            { path: 'role-access', component: RoleAccessComponent }
        ]
    },
    // DEFAULT
    { path: '**', redirectTo: 'login' }
];
