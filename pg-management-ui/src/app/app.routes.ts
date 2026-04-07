import { Routes } from '@angular/router';
import { Login } from './core/auth/login/login';
import { PgSelect } from './core/auth/pg-select/pg-select';
import { Dashboard } from './features/dashboard/dashboard/dashboard';
import { authGuard } from './core/guards/auth-guard';
import { permissionGuard } from './core/guards/permission-guard';
import { Layout } from './shared/layout/layout';
import { AdminLayout } from './admin/admin-layout/admin-layout';
import { adminGuard } from './admin/guards/admin-guard';
import { RegisterPg } from './admin/components/register-pg/register-pg';
import { RoomList } from './features/rooms/room-list/room-list';
import { AddRoom } from './features/rooms/add-room/add-room';
import { TenantList } from './features/tenant/tenant-list/tenant-list';
import { AddTenant } from './features/tenant/add-tenant/add-tenant';
import { RoomDetails } from './features/rooms/room-details/room-details';
import { TenantDetails } from './features/tenant/tenant-details/tenant-details';
import { AddPayment } from './features/payments/add-payment/add-payment';
import { AddPaymentContainer } from './features/payments/add-payment-container/add-payment-container';
import { PaymentsHistory } from './features/payments/payments-history/payments-history';
import { PaymentsLanding } from './features/payments/payments-landing/payments-landing';
import { Expenses } from './features/expenses/expenses/expenses';
import { PaymentDetails } from './features/payments/payment-details/payment-details';
import { BookingList } from './features/bookings/booking-list/booking-list';
import { AddBooking } from './features/bookings/add-booking/add-booking';
import { BookingDetails } from './features/bookings/booking-details/booking-details';
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
            { path: 'dashboard', component: Dashboard },
            { path: 'room-list', component: RoomList, canActivate: [permissionGuard], data: { requiredPermission: 'Room.GetRooms' } },
            { path: 'add-room', component: AddRoom, canActivate: [permissionGuard], data: { requiredPermission: 'Room.CreateRoom' } },
            { path: 'room/:id', component: RoomDetails, canActivate: [permissionGuard], data: { requiredPermission: 'Room.GetRoomById' } },
            { path: 'tenant-list', component: TenantList, canActivate: [permissionGuard], data: { requiredPermission: 'Tenant.GetTenants' } },
            { path: 'tenants/add', component: AddTenant, canActivate: [permissionGuard], data: { requiredPermission: 'Tenant.CreateTenant' } },
            { path: 'tenants/:id', component: TenantDetails, canActivate: [permissionGuard], data: { mode: 'view', requiredPermission: 'Tenant.GetTenantById' } },
            { path: 'tenants/:id/edit', component: TenantDetails, canActivate: [permissionGuard], data: { mode: 'edit', requiredPermission: 'Tenant.UpdateTenant' } },
            {
                path: 'payments',
                children: [
                    { path: '', redirectTo: 'add', pathMatch: 'full' },
                    { path: 'add', component: AddPaymentContainer, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.CreatePayment' } },
                    { path: 'add/:tenantId', component: AddPayment, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.CreatePayment' } },
                    { path: 'history', component: PaymentsLanding, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.GetPaymentHistory' } },
                    { path: ':paymentId', component: PaymentDetails, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.GetPayment' } },
                    { path: ':paymentId/edit', component: PaymentDetails, canActivate: [permissionGuard], data: { requiredPermission: 'Payment.UpdatePayment' } }
                ]
            },
            { path: 'bookings', component: BookingList, canActivate: [permissionGuard], data: { requiredPermission: 'Booking.GetBookings' } },
            { path: 'bookings/add', component: AddBooking, canActivate: [permissionGuard], data: { requiredPermission: 'Booking.CreateBooking' } },
            { path: 'bookings/:id', component: BookingDetails, canActivate: [permissionGuard], data: { mode: 'view', requiredPermission: 'Booking.GetById' } },
            { path: 'bookings/:id/edit', component: BookingDetails, canActivate: [permissionGuard], data: { mode: 'edit', requiredPermission: 'Booking.UpdateBooking' } },
            { path: 'employees', component: EmployeesLanding, canActivate: [permissionGuard], data: { requiredPermission: 'Employee.GetEmployees' } },
            { path: 'expenses', component: Expenses, canActivate: [permissionGuard], data: { requiredPermission: 'Expense.GetExpenses' } },
            { path: 'reports', component: ReportsLanding },
            { path: 'reports/rent-collection', component: RentCollectionReport },
            { path: 'reports/overdue-rent', component: OverdueRentReport },
            { path: 'reports/payment-history', component: PaymentHistoryReport },
            { path: 'reports/occupancy', component: OccupancyReport },
            { path: 'reports/tenant-list', component: TenantListReport },
            { path: 'reports/advance-balance', component: AdvanceBalanceReport },
            { path: 'reports/expenses', component: ExpenseReport },
            { path: 'reports/profit-loss', component: ProfitLossReport },
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
