import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { PermissionService } from '../../../core/services/permission.service';

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
export class ConfigurationsLanding {
  options: ConfigCard[] = [];

  constructor(private auth: Auth, private permissionService: PermissionService) {
    if (this.auth.isOwner() || this.permissionService.hasAccess('PgUser.GetUsers')) {
      this.options.push({
        title: 'Manage Users',
        description: 'Add, remove or update users and assign roles for your PG.',
        icon: '👥',
        route: '/configurations/manage-users'
      });
    }

    if (this.auth.isOwner() || this.permissionService.hasAccess('Settings.GetNotificationSettings')) {
      this.options.push({
        title: 'Notification Settings',
        description: 'Configure automatic payment receipts via email or WhatsApp.',
        icon: '🔔',
        route: '/configurations/notifications'
      });
    }

    if (this.auth.isOwner() || this.permissionService.hasAccess('Settings.GetReportSubscriptions')) {
      this.options.push({
        title: 'Report Subscriptions',
        description: 'Choose which daily reports each user receives via email.',
        icon: '📊',
        route: '/configurations/report-subscriptions'
      });
    }
  }
}
