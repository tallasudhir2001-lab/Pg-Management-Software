import { Component,OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantListDto } from '../models/tenant-list-dto';
import { Tenantservice } from '../services/tenantservice';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-tenant-list',
  standalone:true,
  imports: [CommonModule,FormsModule,RouterLink],
  templateUrl: './tenant-list.html',
  styleUrl: './tenant-list.css',
})
export class TenantList {
tenants: TenantListDto[] = [];
  totalCount = 0;

  // paging
  page = 1;
  pageSize = 10;

  // search & filter
  search = '';
  status: 'all' | 'active' | 'movedout' = 'all';
  roomId: string | null = null;

  // sorting
  sortBy = 'updated';   // backend default
  sortDir: 'asc' | 'desc' = 'desc';

  loading = false;

  constructor(private tenantService: Tenantservice) {}

  loadTenants() {
  this.loading = true;

  this.tenantService.getTenants({
    page: this.page,
    pageSize: this.pageSize,
    search: this.search || null,
    status: this.status !== 'all' ? this.status : null,
    roomId: this.roomId,
    sortBy: this.sortBy,
    sortDir: this.sortDir
  }).subscribe({
    next: res => {
      this.tenants = res.items;
      this.totalCount = res.totalCount;
      this.loading = false;
    },
    error: () => {
      this.loading = false;
    }
  });
}
  ngOnInit() {
    this.loadTenants();
  }
}
