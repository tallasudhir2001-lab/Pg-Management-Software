import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface ReportCard {
  title: string;
  description: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-reports-landing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './reports-landing.html',
  styleUrl: './reports-landing.css'
})
export class ReportsLanding {
  financial: ReportCard[] = [
    {
      title: 'Rent Collection',
      description: 'Monthly rent dues, amounts collected and outstanding balances.',
      icon: '💰',
      route: '/reports/rent-collection'
    },
    {
      title: 'Overdue Rent',
      description: 'Tenants with pending rent as of any given date.',
      icon: '⚠️',
      route: '/reports/overdue-rent'
    },
    {
      title: 'Payment History',
      description: 'Complete log of all payments received in a date range.',
      icon: '🧾',
      route: '/reports/payment-history'
    },
    {
      title: 'Advance Balance',
      description: 'Security deposits held, refunded and net balances per tenant.',
      icon: '🏦',
      route: '/reports/advance-balance'
    },
    {
      title: 'Profit & Loss',
      description: 'Revenue vs expenses summary for any period.',
      icon: '📈',
      route: '/reports/profit-loss'
    }
  ];

  operational: ReportCard[] = [
    {
      title: 'Occupancy',
      description: 'Room-by-room occupancy snapshot as of a selected date.',
      icon: '🏠',
      route: '/reports/occupancy'
    },
    {
      title: 'Tenant List',
      description: 'Directory of all tenants filtered by status.',
      icon: '👥',
      route: '/reports/tenant-list'
    },
    {
      title: 'Expense Report',
      description: 'Categorised breakdown of all expenses in a date range.',
      icon: '📋',
      route: '/reports/expenses'
    }
  ];
}
