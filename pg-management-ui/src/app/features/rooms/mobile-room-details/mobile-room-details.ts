import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Roomservice } from '../services/roomservice';
import { Room } from '../models/room.model';
import { RoomTenant } from '../models/room.tenant.model';

@Component({
  selector: 'app-mobile-room-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-room-details.html',
  styleUrl: './mobile-room-details.css'
})
export class MobileRoomDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private roomService = inject(Roomservice);
  private cdr = inject(ChangeDetectorRef);

  room: Room | null = null;
  tenants: RoomTenant[] = [];
  loading = true;
  error = '';

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadRoom(id);
  }

  loadRoom(id: string) {
    this.loading = true;
    this.roomService.getRoomById(id).subscribe({
      next: (room) => {
        this.room = room;
        this.loading = false;
        this.cdr.detectChanges();
        this.loadTenants(id);
      },
      error: () => {
        this.error = 'Failed to load room';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadTenants(roomId: string) {
    this.roomService.getTenantsByRoom(roomId).subscribe({
      next: (tenants) => {
        this.tenants = tenants;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  goBack() {
    this.router.navigate(['/room-list']);
  }

  openTenant(t: RoomTenant) {
    this.router.navigate(['/tenants', t.tenantId]);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Available': return '#2E7D32';
      case 'Partial': return '#F57F17';
      case 'Full': return '#C62828';
      default: return '#64748b';
    }
  }
}
