import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Roomservice } from '../services/roomservice';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-add-room',
  standalone:true,
  imports: [CommonModule,FormsModule],
  templateUrl: './add-room.html',
  styleUrl: './add-room.css',
})
export class AddRoom {
  model = {
    roomNumber: '',
    capacity: 1,
    rentAmount: 0,
    isAc :false
  };
  isSaving = false;
  errorMessage = '';

  constructor(private router: Router, private roomService : Roomservice,private toastService:ToastService) {}

  save(): void {
    if (!this.model.roomNumber) return;

    this.isSaving = true;
    this.errorMessage = '';

    this.roomService.createRoom(this.model).subscribe({
      next: () => {
      this.toastService.showSuccess('Room Created successfully');
      this.router.navigate(['/room-list']);
      },
      error: err => {
        this.isSaving = false;
        this.errorMessage = err.error || 'Failed to add room';
        this.toastService.showError(this.errorMessage);
      }
    });
  }
  cancel(): void {
    this.router.navigate(['/room-list']);
  }
}
