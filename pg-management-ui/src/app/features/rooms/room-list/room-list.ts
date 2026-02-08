import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Room } from '../models/room.model';
import { FormsModule } from '@angular/forms';
import { Roomservice } from '../services/roomservice';
import { Observable, distinctUntilChanged, switchMap, tap, map } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './room-list.html',
  styleUrl: './room-list.css',
})
export class RoomList {
  rooms$!: Observable<PagedResults<Room>>; 
  pageSize = 9;
  totalPages = 0;
  currentPage = 1;
  pages: number[] = [];
  maxVisiblePages = 5;
  
  searchText = '';
  
  // Filter drawer state
  showFilters = false;
  filterStatus = '';
  filterAc = '';
  filterVacancies = '';

  constructor(
    private router: Router, 
    private roomService: Roomservice, 
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.rooms$ = this.route.queryParamMap.pipe(
      map(params => {
        const page = Number(params.get('page')) || 1;
        const search = params.get('search') || '';
        const status = params.get('status') || '';
        const ac = params.get('ac') || '';
        const vacancies = params.get('vacancies') || '';

        // Sync UI state
        this.currentPage = page;
        this.searchText = search;
        this.filterStatus = status;
        this.filterAc = ac;
        this.filterVacancies = vacancies;

        return { page, search, status, ac, vacancies };
      }),
      distinctUntilChanged(
        (a, b) => JSON.stringify(a) === JSON.stringify(b)
      ),
      switchMap(({ page, search, status, ac, vacancies }) => {
        return this.roomService.getRooms({
          page,
          pageSize: this.pageSize,
          search,
          status,
          ac,
          vacancies
        });
      }),
      tap(result => {
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.buildPages();
      })
    );
  }

  // Pagination methods
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.updateUrl({ page });
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.updateUrl({ page: this.currentPage + 1 });
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.updateUrl({ page: this.currentPage - 1 });
    }
  }

  buildPages(): void {
    const half = Math.floor(this.maxVisiblePages / 2);

    let start = this.currentPage - half;
    let end = this.currentPage + half;

    if (start < 1) {
      start = 1;
      end = Math.min(this.maxVisiblePages, this.totalPages);
    }

    if (end > this.totalPages) {
      end = this.totalPages;
      start = Math.max(1, end - this.maxVisiblePages + 1);
    }

    this.pages = [];
    for (let i = start; i <= end; i++) {
      this.pages.push(i);
    }
  }

  // Search handler
  onSearchChange(value: string): void {
    this.updateUrl({
      search: value ? value : null,
      page: 1
    });
  }

  // Filter drawer methods
  applyFilters(): void {
    this.updateUrl({
      page: 1,
      status: this.filterStatus || null,
      ac: this.filterAc || null,
      vacancies: this.filterVacancies || null
    });

    this.showFilters = false;
  }

  clearFilters(): void {
    this.filterStatus = '';
    this.filterAc = '';
    this.filterVacancies = '';

    this.updateUrl({
      page: 1,
      status: null,
      ac: null,
      vacancies: null
    });

    this.showFilters = false;
  }

  // Helper methods
  getStatusClass(room: Room): string {
    return room.status.toLowerCase(); // available | partial | full
  }

  openRoom(roomId: string) {
    this.router.navigate(['/room', roomId]);
  }

  openAddRoom(): void {
    this.router.navigate(['/add-room']);
  }

  // URL update helper
  private updateUrl(params: {
    page?: number;
    search?: string | null;
    status?: string | null;
    ac?: string | null;
    vacancies?: string | null;
  }): void {
    const cleanParams: any = {};

    Object.keys(params).forEach(key => {
      const value = (params as any)[key];

      if (value === null) {
        cleanParams[key] = null; // Removes param when merging
      } else if (value !== undefined && value !== '') {
        cleanParams[key] = value;
      }
    });

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: cleanParams,
      queryParamsHandling: 'merge'
    });
  }
}
