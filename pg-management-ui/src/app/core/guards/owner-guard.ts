import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';
import { ToastService } from '../../shared/toast/toast-service';

export const ownerGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);
  const toastService = inject(ToastService);

  if (auth.isOwner()) return true;

  toastService.showError('Only the owner can access this page.');
  router.navigate(['/dashboard']);
  return false;
};
