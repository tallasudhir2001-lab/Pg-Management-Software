import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { PermissionService } from '../../../core/services/permission.service';
import { VersionService, AppVersion } from '../../../core/services/version.service';

interface ConfigCard {
  title: string;
  description: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-configurations-landing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './configurations-landing.html',
  styleUrl: './configurations-landing.css'
})
export class ConfigurationsLanding implements OnInit {
  options: ConfigCard[] = [];
  appVersion: AppVersion | null = null;

  constructor(private auth: Auth, private permissionService: PermissionService, private versionService: VersionService) {
    if (this.auth.isOwner() || this.permissionService.hasAccess('PgUser.GetUsers')) {
      this.options.push({
        title: 'Manage Users',
        description: 'Add, remove or update users and assign roles for your PG.',
        icon: '👥',
        route: '/settings/manage-users'
      });
    }

    if (this.auth.isOwner() || this.permissionService.hasAccess('Settings.GetNotificationSettings')) {
      this.options.push({
        title: 'Payment Notifications',
        description: 'Auto-send payment receipts to tenants via email or WhatsApp.',
        icon: '🔔',
        route: '/settings/notifications'
      });
    }

    if (this.auth.isOwner() || this.permissionService.hasAccess('Settings.GetReportSubscriptions')) {
      this.options.push({
        title: 'Report Subscriptions',
        description: 'Choose which daily reports each user receives via email.',
        icon: '📊',
        route: '/settings/report-subscriptions'
      });
    }
  }

  ngOnInit(): void {
    this.versionService.getVersion().subscribe(v => this.appVersion = v);
  }
}
