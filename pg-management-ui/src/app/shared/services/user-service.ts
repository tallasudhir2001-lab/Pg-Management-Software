import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResults } from '../../shared/models/page-results.model';

export interface UserDto {
  userId: string;
  name: string;
  email?: string;
  role?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private readonly baseUrl = `${environment.apiBaseUrl}/users`;

  constructor(private http: HttpClient) {}

  /**
   * Get paginated users list
   */
  getUsers(options: {
    page: number;
    pageSize: number;
    search?: string;
  }): Observable<PagedResults<UserDto>> {
    let params = new HttpParams()
      .set('page', options.page.toString())
      .set('pageSize', options.pageSize.toString());

    if (options.search) {
      params = params.set('search', options.search);
    }

    return this.http.get<PagedResults<UserDto>>(this.baseUrl, { params });
  }

  /**
   * Get single user by ID
   */
  getUserById(userId: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.baseUrl}/${userId}`);
  }
}
