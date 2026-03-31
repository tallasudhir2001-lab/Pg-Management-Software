import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet, RouterLink, NavigationEnd } from '@angular/router';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Auth } from '../../core/services/auth';
import { PermissionService } from '../../core/services/permission.service';
import { BranchViewService } from '../../core/services/branch-view.service';
import { jwtDecode } from 'jwt-decode';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterModule, CommonModule],
  templateUrl: './layout.html',
  styleUrl: './layout.css',
})
export class Layout implements OnInit {
  paymentsExpanded = false;
  userRole: string = '';

  constructor(
    private auth: Auth,
    private router: Router,
    private permissionService: PermissionService,
    public branchView: BranchViewService
  ) {
    const token = this.auth.getToken();
    if (token) {
      try {
        const decoded: any = jwtDecode(token);
        this.userRole = decoded['role'] ?? '';
      } catch { }
    }
  }

  ngOnInit(): void {
    this.branchView.checkCanToggle();
  }

  get isOwner(): boolean {
    return this.userRole === 'Owner';
  }

  get showConfigurations(): boolean {
    return this.isOwner || this.permissionService.hasAccess('Configurations.GetConfigurations');
  }

  togglePayments() {
    this.paymentsExpanded = !this.paymentsExpanded;
  }

  onBranchToggle(): void {
    this.branchView.toggle();
    // Reload current route to refresh data
    const currentUrl = this.router.url;
    this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
      this.router.navigateByUrl(currentUrl);
    });
  }

  logout() {
    this.branchView.reset();
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
