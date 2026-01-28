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

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'select-pg', component: PgSelect },
    {
        path: '',
        component: Layout,
        canActivate: [authGuard],
        children: [
            { path : 'dashboard', component: Dashboard },
            { path :'room-list', component:RoomList},
            { path :'add-room', component:AddRoom},
            { path : 'room/:id',component:RoomDetails},
            { path :'tenant-list',component:TenantList},
            { path :'tenants/add',component:AddTenant},
            { path :'tenants/:id', component:TenantDetails, data : { mode : 'view'}},
            { path :'tenants/:id/edit', component:TenantDetails, data : {mode : 'edit'}},
            { path : 'payments/add/:tenantId', component:AddPayment}
        ]
    },
    {
        path: 'admin',
        component :AdminLayout,
        canActivate :[authGuard,adminGuard],
        children :[
            {path: 'register-pg',component:RegisterPg}
        ]
    },
    // DEFAULT
  { path: '**', redirectTo: 'login' }
];
