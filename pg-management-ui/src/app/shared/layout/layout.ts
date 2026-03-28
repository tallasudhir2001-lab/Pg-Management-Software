import { Component } from '@angular/core';
import { Router, RouterOutlet, RouterLink } from '@angular/router';
import { RouterModule } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { jwtDecode } from 'jwt-decode';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterModule],
  templateUrl: './layout.html',
  styleUrl: './layout.css',
})
export class Layout {
  paymentsExpanded = false;
  userRole: string = '';

  constructor(private auth: Auth, private router: Router) {
    const token = this.auth.getToken();
    if (token) {
      try {
        const decoded: any = jwtDecode(token);
        this.userRole = decoded['role'] ?? '';
      } catch { }
    }
  }

  get isOwner(): boolean {
    return this.userRole === 'Owner';
  }

  togglePayments() {
    this.paymentsExpanded = !this.paymentsExpanded;
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
