import { Component,OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Room } from '../models/room.model';
import { FormsModule } from '@angular/forms';
import { Roomservice } from '../services/roomservice';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-room-list',
  standalone:true,
  imports: [CommonModule,FormsModule],
  templateUrl: './room-list.html',
  styleUrl: './room-list.css',
})
export class RoomList {
  rooms$!: Observable<Room[]>;
  searchRoomNumber = '';
  selectedStatus = '';
  selectedAcType = '';
  showAddRoom = false;

  newRoom = {
    roomNumber: '',
    capacity: 1,
    rentAmount: 0
  };
 
  constructor(private router: Router, private roomService: Roomservice) {}

  ngOnInit(): void {
      this.rooms$ = this.roomService.getRooms();
  }

getStatusClass(room: Room): string {
  return room.status.toLowerCase(); // available | partial | full
}

  // getVacancy(room: any): number {
  //   return room.capacity - room.occupied;
  // }


  openRoom(roomId: string) {
    this.router.navigate(['/room', roomId]);
  }

  applyFilters(rooms: Room[]): Room[] {
    return rooms.filter(room => {
      const matchesRoom =
        !this.searchRoomNumber ||
        room.roomNumber.includes(this.searchRoomNumber.trim());

      const matchesStatus =
        !this.selectedStatus ||
        room.status.toLowerCase() === this.selectedStatus;

        const matchesAc =
      !this.selectedAcType ||
      (this.selectedAcType === 'ac' && room.isAc) ||
      (this.selectedAcType === 'non-ac' && !room.isAc);

      return matchesRoom && matchesStatus && matchesAc;
    });
  }
  openAddRoom(): void {
  this.showAddRoom = true;
  this.router.navigate(['/add-room']);
}

// cancelAddRoom(): void {
//   this.showAddRoom = false;
// }

addRoom(): void {
  this.roomService.createRoom(this.newRoom).subscribe({
    next: () => {
      this.showAddRoom = false;

      this.rooms$ = this.roomService.getRooms();
    },
    error: (err: { error: any; }) => {
      console.error(err);
      alert(err.error || 'Failed to add room');
    }
  });
}
}
