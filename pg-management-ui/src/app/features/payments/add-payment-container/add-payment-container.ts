import { Component } from '@angular/core';
import { TenantListDto } from '../../tenant/models/tenant-list-dto';
import { Tenantservice } from '../../tenant/services/tenantservice';
import { AddPayment } from '../add-payment/add-payment';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-add-payment-container',
  imports: [AddPayment,CommonModule, FormsModule],
  templateUrl: './add-payment-container.html',
  styleUrl: './add-payment-container.css',
})
export class AddPaymentContainer {

  search = '';
  tenants: TenantListDto[] = [];
  selectedTenant: TenantListDto | null = null;
  loading = false;

  constructor(private tenantService: Tenantservice) {}

  onSearchChange() {
    if (this.search.trim().length < 2) {
      this.tenants = [];
      return;
    }

    this.loading = true;

    this.tenantService.getTenants({
      search: this.search,
      page: 1,
      pageSize: 5
    }).subscribe({
      next: res => {
        this.tenants = res.items;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  selectTenant(t: TenantListDto) {
    this.selectedTenant = t;
    this.search = `${t.name}${t.roomNumber ? ' (Room ' + t.roomNumber + ')' : ''}`;
    this.tenants = [];
  }

  clearTenant() {
    this.selectedTenant = null;
    this.search = '';
  }

}
