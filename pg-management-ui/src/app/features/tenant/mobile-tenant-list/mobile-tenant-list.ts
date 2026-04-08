import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Tenantservice } from '../services/tenantservice';
import { TenantListDto } from '../models/tenant-list-dto';
import { Subject, debounceTime, distinctUntilChanged, filter, Subscription } from 'rxjs';

@Component({
  selector: 'app-mobile-tenant-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mobile-tenant-list.html',
  styleUrl: './mobile-tenant-list.css'
})
export class MobileTenantList implements OnInit, OnDestroy {
  private tenantService = inject(Tenantservice);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  tenants: TenantListDto[] = [];
  loading = true;
  error = '';
  activeFilter = 'all';
  searchText = '';
  totalCount = 0;

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  ngOnInit() {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      filter(text => !text.trim() || text.trim().length >= 3)
    ).subscribe(() => this.loadTenants());

    this.loadTenants();
  }

  ngOnDestroy() {
    this.searchSub?.unsubscribe();
  }

  loadTenants() {
    this.loading = true;
    this.error = '';

    const params: any = { page: 1, pageSize: 100 };
    if (this.searchText.trim()) {
      params.search = this.searchText.trim();
    }
    if (this.activeFilter === 'ACTIVE' || this.activeFilter === 'MOVED OUT') {
      params.status = this.activeFilter;
    }
    if (this.activeFilter === 'RENT_PENDING') {
      params.rentPending = true;
    }

    this.tenantService.getTenants(params).subscribe({
      next: (result) => {
        this.tenants = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load tenants';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  setFilter(filter: string) {
    this.activeFilter = filter;
    this.loadTenants();
  }

  onSearch() {
    this.searchSubject.next(this.searchText);
  }

  clearSearch() {
    this.searchText = '';
    this.loadTenants();
  }

  addTenant() {
    this.router.navigate(['/tenants/add']);
  }

  openTenant(tenant: TenantListDto) {
    this.router.navigate(['/tenants', tenant.tenantId]);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'ACTIVE': return '#2E7D32';
      case 'MOVED OUT': return '#64748b';
      default: return '#F57F17';
    }
  }

  getInitial(name: string): string {
    return name ? name.charAt(0).toUpperCase() : '?';
  }
}
