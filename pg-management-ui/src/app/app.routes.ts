import { Routes } from '@angular/router';
import { Login } from './core/auth/login/login';
import { PgSelect } from './core/auth/pg-select/pg-select';
import { Dashboard } from './features/dashboard/dashboard/dashboard';
import { authGuard } from './core/guards/auth-guard';
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
import { Expenses } from './features/expenses/expenses/expenses';
import { PaymentDetails } from './features/payments/payment-details/payment-details';
import { BookingList } from './features/bookings/booking-list/booking-list';
import { AddBooking } from './features/bookings/add-booking/add-booking';
import { BookingDetails } from './features/bookings/booking-details/booking-details';

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
            { path: 'room-list', component: RoomList },
            { path: 'add-room', component: AddRoom },
            { path: 'room/:id', component: RoomDetails },
            { path: 'tenant-list', component: TenantList },
            { path: 'tenants/add', component: AddTenant },
            { path: 'tenants/:id', component: TenantDetails, data: { mode: 'view' } },
            { path: 'tenants/:id/edit', component: TenantDetails, data: { mode: 'edit' } },
            {
                path: 'payments',
                children: [
                    { path: '', redirectTo: 'add', pathMatch: 'full' },
                    { path: 'add', component: AddPaymentContainer },
                    { path: 'add/:tenantId', component: AddPayment },
                    { path: 'history', component: PaymentsHistory },
                    { path: ':paymentId', component: PaymentDetails },
                    { path: ':paymentId/edit', component: PaymentDetails }
                ]
            },
            { path: 'bookings', component: BookingList },
            { path: 'bookings/add', component: AddBooking },
            { path: 'bookings/:id', component: BookingDetails, data: { mode: 'view' } },
            { path: 'bookings/:id/edit', component: BookingDetails, data: { mode: 'edit' } },
            { path: 'expenses', component: Expenses}

        ]
    },
    {
        path: 'admin',
        component: AdminLayout,
        canActivate: [authGuard, adminGuard],
        children: [
            { path: 'register-pg', component: RegisterPg }
        ]
    },
    // DEFAULT
    { path: '**', redirectTo: 'login' }
];
