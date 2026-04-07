import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Roomservice } from '../services/roomservice';
import { Room } from '../models/room.model';

@Component({
  selector: 'app-mobile-room-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mobile-room-list.html',
  styleUrl: './mobile-room-list.css'
})
export class MobileRoomList implements OnInit {
  private roomService = inject(Roomservice);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  rooms: Room[] = [];
  loading = true;
  error = '';
  activeFilter = 'all';
  totalCount = 0;

  ngOnInit() {
    this.loadRooms();
  }

  loadRooms() {
    this.loading = true;
    this.error = '';

    const params: any = { page: 1, pageSize: 100 };
    if (this.activeFilter !== 'all') {
      params.status = this.activeFilter;
    }

    this.roomService.getRooms(params).subscribe({
      next: (result) => {
        this.rooms = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load rooms';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  setFilter(filter: string) {
    this.activeFilter = filter;
    this.loadRooms();
  }

  addRoom() {
    this.router.navigate(['/add-room']);
  }

  openRoom(room: Room) {
    this.router.navigate(['/room', room.roomId]);
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
