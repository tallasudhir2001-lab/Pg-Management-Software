import { CanActivateFn,Router } from '@angular/router';
import { inject } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

export const adminGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');

  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  try {
    const decoded: any = jwtDecode(token);

    // Role may come in different claim formats
    const role =
      decoded.role ||
      decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    if (role === 'Admin') {
      return true;
    }
    // Logged in but not admin
    router.navigate(['/dashboard']);
    return false;

  } catch (e) {
    router.navigate(['/login']);
    return false;
  }
};
