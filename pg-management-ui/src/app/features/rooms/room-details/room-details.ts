import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { Roomservice } from '../services/roomservice';
import { Room } from '../models/room.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { RoomTenant } from '../models/room.tenant.model';

@Component({
  selector: 'app-room-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './room-details.html',
  styleUrl: './room-details.css',
})
export class RoomDetails implements OnInit {
  room$!: Observable<Room>;
  tenants$!: Observable<RoomTenant[]>;

  model!: Room;
  isSaving = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private roomService: Roomservice,
    private cdr: ChangeDetectorRef,
    private toastService: ToastService
  ) { }

  ngOnInit(): void {
    const roomId = this.route.snapshot.paramMap.get('id')!;
    this.room$ = this.roomService.getRoomById(roomId).pipe(
      tap(room => {
        this.model = { ...room };
      })
    );
    this.tenants$ = this.roomService.getTenantsByRoom(roomId);
  }
  save(): void {
    this.isSaving = true;

    this.roomService.updateRoom(this.model.roomId, {
      roomNumber: this.model.roomNumber,
      capacity: this.model.capacity,
      rentAmount: this.model.rentAmount,
      isAc: this.model.isAc
    }).subscribe({
      next: () => {
        this.toastService.showSuccess('Changes Saved Successfully.');
        this.router.navigate(['/room-list']);
      },
      error: (err: { error: string; }) => {
        this.isSaving = false;
        this.error = err.error || 'Update failed';
        this.toastService.showError(this.error);
      }
    });
  }
  delete(): void {
    if (!confirm('Are you sure you want to delete this room?')) return;

    this.roomService.deleteRoom(this.model.roomId).subscribe({
      next: () => this.router.navigate(['/room-list']),
      error: (err: { error: string; }) => {
        this.error = err.error || 'Delete failed';
        this.toastService.showError(this.error);
        this.cdr.detectChanges(); // 3. Force the UI to update immediately
      }
    });
  }
  cancel(): void {
    this.router.navigate(['/room-list']);
  }
  onAction(action: 'view' | 'edit', tenantId: string): void {
    switch (action) {
      case 'view':
        this.viewTenant(tenantId);
        break;
      case 'edit':
        this.editTenant(tenantId);
        break;
    }
  }
  private viewTenant(tenantId: string): void {
    this.router.navigate(['/tenants', tenantId]);
  }
  private editTenant(tenantId: string): void {
    this.router.navigate(['/tenants', tenantId, 'edit']);
  }
}
