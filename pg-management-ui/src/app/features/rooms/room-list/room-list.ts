import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Room } from '../models/room.model';
import { FormsModule } from '@angular/forms';
import { Roomservice } from '../services/roomservice';
import { BehaviorSubject, combineLatest, debounceTime, distinctUntilChanged, Observable, switchMap, tap, map } from 'rxjs';
import { PagedResults } from '../../../shared/models/page-results.model';
import { ActivatedRoute } from '@angular/router';
import { PaginationHelper } from '../../../shared/utils/pagination.helper';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './room-list.html',
  styleUrl: './room-list.css',
})
export class RoomList {
   //used for add-room
  currentPage$ = new BehaviorSubject<number>(1);
   newRoom = {
    roomNumber: '',
    capacity: 1,
    rentAmount: 0
  };

  //for pagination and filtering
  rooms$!: Observable<PagedResults<Room>>; 
  pageSize = 9;
  totalPages = 0;
  currentPage = 1;
  pages: number[] = [];
  maxVisiblePages = 5;
  // search$ = new BehaviorSubject<string>('');
  // status$ = new BehaviorSubject<string>('');
  // ac$ = new BehaviorSubject<string>('');
  searchText = '';
  selectedStatus = '';
  selectedAc = '';

  //searchRoomNumber = '';
  //selectedAcType = '';
  showAddRoom = false;

 

  constructor(private router: Router, private roomService: Roomservice, private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.rooms$ = this.route.queryParamMap.pipe(
      map(params => ({
        page: Number(params.get('page')) || 1,
        search: params.get('search') || '',
        status: params.get('status') || '',
        ac: params.get('ac') || ''
      })),
      distinctUntilChanged(
        (a, b) => JSON.stringify(a) === JSON.stringify(b)
      ),
      switchMap(({ page, search, status, ac }) => {
        this.currentPage = page; // ✅ set ONLY here

        return this.roomService.getRooms({
          page,
          pageSize: this.pageSize,
          search,
          status,
          ac
        });
      }),
      tap(result => {
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.buildPages();
      })
    );
  }


  //   private buildRoomsStream(): void {
  //   this.rooms$ = combineLatest([
  //     this.currentPage$,
  //     this.search$.pipe(
  //       debounceTime(300),
  //       distinctUntilChanged()
  //     ),
  //     this.status$,
  //     this.ac$
  //   ]).pipe(
  //     switchMap(([page, search, status, ac]) => {
  //   this.currentPage = page; //  source of truth
  //   return this.roomService.getRooms({
  //     page,
  //     pageSize: this.pageSize,
  //     search,
  //     status,
  //     ac
  //   });
  // }),
  //     tap(result => {
  //   this.totalPages = Math.ceil(result.totalCount / this.pageSize);
  //   this.buildPages();
  // })

  //   );
  // }
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.updateUrl({ page });

    // PaginationHelper.updateQueryParams(
    //   this.router,
    //   this.route,
    //   {
    //     page: this.currentPage$.value,
    //     search: this.search$.value,
    //     status: this.status$.value,
    //     ac: this.ac$.value
    //   }
    // );
    //   this.currentPage$.next(page);
  }

  nextPage(): void {
    this.updateUrl({ page: this.currentPage + 1 });
  }

  prevPage(): void {
    this.updateUrl({ page: this.currentPage - 1 });
  }

  getStatusClass(room: Room): string {
    return room.status.toLowerCase(); // available | partial | full
  }

  openRoom(roomId: string) {
    this.router.navigate(['/room', roomId]);
  }

  openAddRoom(): void {
    this.showAddRoom = true;
    this.router.navigate(['/add-room']);
  }

  buildPages(): void {
    const half = Math.floor(this.maxVisiblePages / 2);

    let start = this.currentPage - half;
    let end = this.currentPage + half;

    // Adjust if start is before 1
    if (start < 1) {
      start = 1;
      end = Math.min(this.maxVisiblePages, this.totalPages);
    }

    // Adjust if end exceeds total pages
    if (end > this.totalPages) {
      end = this.totalPages;
      start = Math.max(1, end - this.maxVisiblePages + 1);
    }

    this.pages = [];
    for (let i = start; i <= end; i++) {
      this.pages.push(i);
    }
  }
  onSearchChange(value: string): void {
    this.updateUrl({
      search: value,
      page: 1
    });
  }
  onStatusChange(value: string): void {
    this.updateUrl({
      status: value,
      page: 1
    });
  }
  onAcChange(value: string): void {
    this.updateUrl({
      ac: value,
      page: 1
    });
  }

  private updateUrl(params: {
    page?: number;
    search?: string;
    status?: string;
    ac?: string;
  }): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      queryParamsHandling: 'merge'
    });
  }
  // private syncUrl(): void {
  //     PaginationHelper.updateQueryParams(
  //       this.router,
  //       this.route,
  //       {
  //         page: this.currentPage$.value,
  //         search: this.search$.value,
  //         status: this.status$.value,
  //         ac: this.ac$.value
  //       }
  //     );
  //   }

  //frontend filtering logic, not needed as we implemented server side pagination
  // applyFilters(rooms: Room[]): Room[] {
  //   return rooms.filter(room => {
  //     const matchesRoom =
  //       !this.searchRoomNumber ||
  //       room.roomNumber.includes(this.searchRoomNumber.trim());

  //     const matchesStatus =
  //       !this.selectedStatus ||
  //       room.status.toLowerCase() === this.selectedStatus;

  //     const matchesAc =
  //       !this.selectedAcType ||
  //       (this.selectedAcType === 'ac' && room.isAc) ||
  //       (this.selectedAcType === 'non-ac' && !room.isAc);

  //     return matchesRoom && matchesStatus && matchesAc;
  //   });
  // }

  // addRoom(): void {
  //   this.roomService.createRoom(this.newRoom).subscribe({
  //     next: () => {
  //       this.showAddRoom = false;

  //       // ✅ trigger reload via pagination stream
  //       this.currentPage$.next(1);
  //     },
  //     error: err => {
  //       console.error(err);
  //       alert(err.error || 'Failed to add room');
  //     }
  //   });
  // }
}
