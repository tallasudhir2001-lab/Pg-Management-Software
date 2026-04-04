import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Auth } from '../../core/services/auth';
import { VersionService } from '../../core/services/version.service';

@Component({
  selector: 'app-admin-layout',
  standalone:true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css',
})
export class AdminLayout implements OnInit {
  appVersion: string = '';

  constructor(
    private auth: Auth,
    private router: Router,
    private versionService: VersionService
  ) {}

  ngOnInit(): void {
    this.versionService.getVersion().subscribe(v => this.appVersion = v.version);
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
