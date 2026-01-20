import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { Roomservice } from '../services/roomservice';
import { Room } from '../models/room.model';

@Component({
  selector: 'app-room-details',
  standalone:true,
  imports: [CommonModule, FormsModule],
  templateUrl: './room-details.html',
  styleUrl: './room-details.css',
})
export class RoomDetails implements OnInit{
  room$!: Observable<Room>;

  model!: Room;
  isSaving = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private roomService: Roomservice
  ) {}

  ngOnInit(): void {
    const roomId = this.route.snapshot.paramMap.get('id')!;
    this.room$ = this.roomService.getRoomById(roomId).pipe(
    tap(room => {
      this.model = { ...room };
    })
  );
  }
  save(): void {
    this.isSaving = true;

    this.roomService.updateRoom(this.model.roomId, {
      roomNumber: this.model.roomNumber,
      capacity: this.model.capacity,
      rentAmount: this.model.rentAmount,
      isAc: this.model.isAc
    }).subscribe({
      next: () => this.router.navigate(['/room-list']),
      error: (err: { error: string; }) => {
        this.isSaving = false;
        this.error = err.error || 'Update failed';
      }
    });
  }
  delete(): void {
    if (!confirm('Are you sure you want to delete this room?')) return;

    this.roomService.deleteRoom(this.model.roomId).subscribe({
      next: () => this.router.navigate(['/room-list']),
      error: (err: { error: string; }) => this.error = err.error || 'Delete failed'
    });
  }
  cancel():void{
    this.router.navigate(['/room-list']);
  }
}
