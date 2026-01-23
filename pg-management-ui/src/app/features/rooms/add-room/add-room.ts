import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Roomservice } from '../services/roomservice';

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
    isAc :0
  };
  isSaving = false;
  errorMessage = '';

  constructor(private router: Router, private roomService : Roomservice) {}

  save(): void {
    if (!this.model.roomNumber) return;

    this.isSaving = true;
    this.errorMessage = '';

    this.roomService.createRoom(this.model).subscribe({
      next: () => {
      this.router.navigate(['/room-list']);
      },
      error: err => {
        this.isSaving = false;
        this.errorMessage = err.error || 'Failed to add room';
      }
    });
  }
  cancel(): void {
    this.router.navigate(['/room-list']);
  }
}
