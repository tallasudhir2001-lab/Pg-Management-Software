import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Room } from '../models/room.model';
import { environment } from '../../../../environments/environment';
import { PagedResults } from '../../../shared/models/page-results.model';

@Injectable({
  providedIn: 'root',
})
export class Roomservice {
  private readonly baseUrl = `${environment.apiBaseUrl}/rooms`;

  constructor(private http: HttpClient) {}

 getRooms(params: {
  page: number;
  pageSize: number;
  search?: string;
  status?: string;
  ac?: string;
}) {
  const query = new HttpParams()
    .set('page', params.page)
    .set('pageSize', params.pageSize)
    .set('search', params.search || '')
    .set('status', params.status || '')
    .set('ac', params.ac || '');

  return this.http.get<PagedResults<Room>>(`${this.baseUrl}`, { params: query });
}


  createRoom(payload: {
  roomNumber: string;
  capacity: number;
  rentAmount: number;
  isAc: boolean;
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
