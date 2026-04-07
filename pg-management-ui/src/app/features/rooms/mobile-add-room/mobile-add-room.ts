import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Roomservice } from '../services/roomservice';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-mobile-add-room',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mobile-add-room.html',
  styleUrl: './mobile-add-room.css'
})
export class MobileAddRoom {
  private router = inject(Router);
  private roomService = inject(Roomservice);
  private toastService = inject(ToastService);

  model = { roomNumber: '', capacity: 1, rentAmount: 0, isAc: false };
  isSaving = false;

  save() {
    if (!this.model.roomNumber?.trim()) {
      this.toastService.showError('Room number is required.');
      return;
    }
    this.isSaving = true;
    this.roomService.createRoom(this.model).subscribe({
      next: () => {
        this.toastService.showSuccess('Room created successfully.');
        this.router.navigate(['/room-list']);
      },
      error: (err) => {
        this.isSaving = false;
        this.toastService.showError(err.error || 'Failed to create room');
      }
    });
  }

  cancel() {
    this.router.navigate(['/room-list']);
  }
}
