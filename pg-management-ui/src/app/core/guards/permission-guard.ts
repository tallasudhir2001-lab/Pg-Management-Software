import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from '../services/permission.service';
import { ToastService } from '../../shared/toast/toast-service';

export const permissionGuard: CanActivateFn = (route) => {
  const permission = route.data?.['requiredPermission'] as string | undefined;
  if (!permission) return true;

  const permissionService = inject(PermissionService);
  const router = inject(Router);
  const toastService = inject(ToastService);

  if (permissionService.hasAccess(permission)) return true;

  toastService.showError('You do not have permission to access this page.');
  router.navigate(['/dashboard']);
  return false;
};
