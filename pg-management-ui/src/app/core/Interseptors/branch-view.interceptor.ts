import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BranchViewService } from '../services/branch-view.service';

export const branchViewInterceptor: HttpInterceptorFn = (req, next) => {
  const branchView = inject(BranchViewService);

  if (branchView.isActive) {
    const cloned = req.clone({
      setHeaders: { 'X-Branch-View': 'true' }
    });
    return next(cloned);
  }

  return next(req);
};
