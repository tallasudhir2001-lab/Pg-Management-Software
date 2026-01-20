import { Component,OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-list',
  standalone:true,
  imports: [CommonModule],
  templateUrl: './tenant-list.html',
  styleUrl: './tenant-list.css',
})
export class TenantList {
  tenants: any[] = [];

  ngOnInit() {
    this.tenants = [
      { name: 'Ravi', room: '101', phone: '9999999999' },
      { name: 'Anil', room: '102', phone: '8888888888' }
    ];
  }
}
