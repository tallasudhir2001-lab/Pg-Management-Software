import { Component, OnInit, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuditService, AuditListResponse } from '../services/audit.service';
import { AuditEvent } from '../models/audit-event.model';
import { PermissionService } from '../../../core/services/permission.service';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditLog implements OnInit {
  events: AuditEvent[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  statusFilter = '';
  entityTypeFilter = '';
  eventTypeFilter = '';
  loading = false;
  expandedId: string | null = null;

  readonly eventTypeLabels: Record<string, string> = {
    PAYMENT_AMOUNT_CHANGED: 'Payment Amount Changed',
    PAYMENT_PERIOD_CHANGED: 'Payment Period Changed',
    PAYMENT_DELETED: 'Payment Deleted',
    EXPENSE_AMOUNT_CHANGED: 'Expense Amount Changed',
    EXPENSE_DELETED: 'Expense Deleted',
    ROOM_RENT_CHANGED: 'Room Rent Changed',
    ADVANCE_SETTLED: 'Advance Settled',
  };

  readonly eventTypeColors: Record<string, string> = {
    PAYMENT_AMOUNT_CHANGED: 'amber',
    PAYMENT_PERIOD_CHANGED: 'amber',
    PAYMENT_DELETED: 'red',
    EXPENSE_AMOUNT_CHANGED: 'amber',
    EXPENSE_DELETED: 'red',
    ROOM_RENT_CHANGED: 'blue',
    ADVANCE_SETTLED: 'green',
  };

  constructor(private auditService: AuditService, private router: Router, private cdr: ChangeDetectorRef, private permissionService: PermissionService) {}

  get canReview(): boolean {
    return this.permissionService.hasAccess('Audit.MarkAsReviewed');
  }

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {
    this.loading = true;
    this.auditService
      .getAuditEvents({
        page: this.page,
        pageSize: this.pageSize,
        eventType: this.eventTypeFilter || undefined,
        entityType: this.entityTypeFilter || undefined,
        status: this.statusFilter || undefined,
      })
      .subscribe({
        next: (res: AuditListResponse) => {
          this.events = res.items;
          this.totalCount = res.totalCount;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadEvents();
  }

  markAsReviewed(event: AuditEvent, e: Event): void {
    e.stopPropagation();
    this.auditService.markAsReviewed(event.id).subscribe(() => {
      event.isReviewed = true;
      this.cdr.detectChanges();
    });
  }

  markAllReviewed(): void {
    this.auditService.markAllAsReviewed().subscribe(() => {
      this.loadEvents();
      this.cdr.detectChanges();
    });
  }

  toggleExpand(id: string): void {
    this.expandedId = this.expandedId === id ? null : id;
    this.cdr.detectChanges();
  }

  parseJson(val: string | null): Record<string, any> | null {
    if (!val) return null;
    try {
      return JSON.parse(val);
    } catch {
      return null;
    }
  }

  objectKeys(obj: Record<string, any>): string[] {
    return Object.keys(obj);
  }

  getEventLabel(type: string): string {
    return this.eventTypeLabels[type] ?? type;
  }

  getEventColor(type: string): string {
    return this.eventTypeColors[type] ?? 'grey';
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get unreviewedCount(): number {
    return this.events.filter((e) => !e.isReviewed).length;
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadEvents();
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
