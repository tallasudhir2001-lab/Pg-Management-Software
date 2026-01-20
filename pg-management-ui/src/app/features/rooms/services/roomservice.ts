import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Room } from '../models/room.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class Roomservice {
  private readonly baseUrl = `${environment.apiBaseUrl}/rooms`;

  constructor(private http: HttpClient) {}

  getRooms(): Observable<Room[]> {
    return this.http.get<Room[]>(this.baseUrl);
  }
  createRoom(payload: {
  roomNumber: string;
  capacity: number;
  rentAmount: number;
  }) {
  return this.http.post(`${this.baseUrl}/add-room`, payload);
  }
  getRoomById(roomId: string) {
    return this.http.get<Room>(`${this.baseUrl}/${roomId}`);
  }

  updateRoom(roomId: string, payload: any) {
    return this.http.put(`${this.baseUrl}/${roomId}`, payload);
  }

  deleteRoom(roomId: string) {
    return this.http.delete(`${this.baseUrl}/${roomId}`);
  }

}
