import { Component,HostListener,OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantListDto } from '../models/tenant-list-dto';
import { Tenantservice } from '../services/tenantservice';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PagedResults } from '../../../shared/models/page-results.model';
import { distinctUntilChanged, map, Observable, switchMap, tap } from 'rxjs';
import { Roomservice } from '../../rooms/services/roomservice';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-tenant-list',
  standalone:true,
  imports: [CommonModule,FormsModule,RouterLink],
  templateUrl: './tenant-list.html',
  styleUrl: './tenant-list.css',
})
export class TenantList implements OnInit{
  tenants$!: Observable<PagedResults<TenantListDto>>;

  //pagination
  pageSize = 10;
  currentPage = 1;
  totalPages = 0;
  pages: number[] = [];
  maxVisiblePages = 5;

  searchText = '';

  showFilters = false;
  rooms: RoomFilterItem[] = [];
  filteredRooms: RoomFilterItem[] = [];

  roomSearchText = '';
  selectedRoomId: string | null = null;
  isRoomDropdownOpen = false;
  selectedRoomLabel = '';
  filterStatus: '' | 'active' | 'movedout' = '';
  filterRentPending: '' | 'true' | 'false' = '';

  //sorting
  sortBy = 'updated';
  sortDir: 'asc' | 'desc' = 'desc';

  //pagination count
  totalCount = 0;




  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tenantService: Tenantservice,
    private roomService :Roomservice,
    private toastService : ToastService
  ) {}

  ngOnInit(): void {
    //for room filters dropdown
    this.loadRoomsForFilter();

    this.tenants$ = this.route.queryParamMap.pipe(
      map(params => {
        const page = Number(params.get('page')) || 1;
        const search = params.get('search') || '';
        const tenantStatus  = params.get('status') || '';
        const roomIdParam = params.get('roomId');
        const roomId = roomIdParam ? roomIdParam : null;
        const rentPendingParam = params.get('rentPending');
        const rentPending = rentPendingParam ? rentPendingParam : null;
        const sortByParam = params.get('sortBy') || 'updated';
        const sortDirParam = (params.get('sortDir') as 'asc' | 'desc') || 'desc';
        
        //  sync UI
        this.currentPage = page;
        this.searchText = search;
        this.filterStatus = tenantStatus as any;
        this.selectedRoomId = roomId;
        this.filterRentPending = (rentPending || '') as any;
        this.sortBy = sortByParam;
        this.sortDir = sortDirParam;

        // restoring selected room label to show again when user opens filters
        if (roomId) {
          const room = this.rooms.find(r => r.roomId === roomId);
          this.selectedRoomLabel = room ? `Room ${room.roomNumber}` : '';
        } else {
          this.selectedRoomLabel = '';
        }

        return { page, search, tenantStatus, roomId, rentPending, sortBy: sortByParam, sortDir: sortDirParam};
      }),
      distinctUntilChanged(
        (a, b) => JSON.stringify(a) === JSON.stringify(b)
      ),
      switchMap(({ page, search, tenantStatus, roomId, rentPending, sortBy, sortDir }) =>
        this.tenantService.getTenants({
          page,
          pageSize: this.pageSize,
          search,
          status:tenantStatus,
          roomId:roomId ?? undefined,
          rentPending: rentPending === 'true' ? true : rentPending === 'false' ? false : undefined,
          sortBy,
          sortDir
        })
      ),
      tap(result => {
          this.totalCount = result.totalCount;
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.buildPages();
      })
    );
  }

  // 🔍 Search
  onSearchChange(value: string): void {
    this.updateUrl({ search: value?value:null, page: 1 });
  }

  //  Pagination helper methods -- start
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.updateUrl({ page });
  }
  nextPage(): void {
  if (this.currentPage < this.totalPages) {
    this.goToPage(this.currentPage + 1);
  }
}

prevPage(): void {
  if (this.currentPage > 1) {
    this.goToPage(this.currentPage - 1);
  }
}
// pagination helper methods --end
  applyFilters(): void {
  this.updateUrl({
    page: 1,
    status: this.filterStatus || null,
    roomId: this.selectedRoomId || null,
    rentPending: this.filterRentPending || null
  });

  this.showFilters = false;
}

  clearFilters(): void {
  this.filterStatus = '';
  this.selectedRoomId = null;
  this.selectedRoomLabel = '';
  this.roomSearchText = '';
  this.filterRentPending = '';

  this.updateUrl({ page: 1,status:null,roomId:null,rentPending:null,sortBy:null,sortDir:null });
  this.showFilters = false;
}

  private updateUrl(params: {
  page?: number;
  search?: string | null;
  status?: string | null;
  roomId?: string | null;
  rentPending?: string | null;
  sortBy?: string | null;
  sortDir?: string | null;
}): void {

  const cleanParams: any = {};

  Object.keys(params).forEach(key => {
    const value = (params as any)[key];

    if (value === null) {
      cleanParams[key] = null; // 🔒 removes param when merging
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

  private loadRoomsForFilter(): void {
  this.roomService.getRooms({
    page: 1,
    pageSize: 200 // enough for dropdown
  }).subscribe(res => {
    this.rooms = res.items.map(r => ({
      roomId: r.roomId,
      roomNumber: r.roomNumber
    }));

    // initially, all rooms visible
    this.filteredRooms = [...this.rooms];
  });
}
onRoomInput(value: string): void {
  // User started typing again → clear selection
  this.selectedRoomId = null;
  this.selectedRoomLabel = '';
  this.roomSearchText = value;

  const search = value.toLowerCase();
  this.filteredRooms = this.rooms.filter(r =>
    r.roomNumber.toLowerCase().includes(search)
  );

  this.isRoomDropdownOpen = true;
}

selectRoom(room: { roomId: string; roomNumber: string }): void {
  this.selectedRoomId = room.roomId;
  this.selectedRoomLabel = `Room ${room.roomNumber}`;
  this.roomSearchText = '';
  this.isRoomDropdownOpen = false;
}

@HostListener('document:click', ['$event'])
onOutsideClick(event: MouseEvent) {
  const target = event.target as HTMLElement;
  if (!target.closest('.searchable-dropdown')) {
    this.isRoomDropdownOpen = false;
  }
}

onSort(column: string): void {
  let direction: 'asc' | 'desc' = 'asc';

  if (this.sortBy === column) {
    // toggle direction
    direction = this.sortDir === 'asc' ? 'desc' : 'asc';
  } else {
    // new column → default direction
    direction = column === 'updated' ? 'desc' : 'asc';
  }

  this.updateUrl({
    page: 1,
    sortBy: column,
    sortDir: direction
  });
}
//sort icon helper methods -- start
isSortedBy(column: string): boolean {
  return this.sortBy === column;
}

isSortAsc(column: string): boolean {
  return this.sortBy === column && this.sortDir === 'asc';
}
//sort icon helper methods -- end

//single action dispatcher method
onAction(action: 'view' | 'edit' | 'delete', tenant: TenantListDto): void {
  switch (action) {
    case 'view':
      this.viewTenant(tenant.tenantId);
      break;

    case 'edit':
      this.editTenant(tenant.tenantId);
      break;

    case 'delete':
      this.confirmDeleteTenant(tenant);
      break;
  }
}

//tenant action helpers start
private viewTenant(tenantId: string): void {
  this.router.navigate(['/tenants', tenantId]);
}
private editTenant(tenantId: string): void {
  this.router.navigate(['/tenants', tenantId, 'edit']);
}
private confirmDeleteTenant(tenant: TenantListDto): void {
  const confirmed = confirm(
    `Are you sure you want to delete tenant "${tenant.name}"?`
  );

  if (!confirmed) return;

  this.deleteTenant(tenant.tenantId);
}

private deleteTenant(tenantId: string): void {
  this.tenantService.deleteTenant(tenantId).subscribe({
    next: () => {
      // URL-driven refresh 
      this.toastService.showSuccess('Tenant Deleted Successfully.');
      this.router.navigate([], {
        relativeTo: this.route,
        queryParamsHandling: 'preserve'
      });
    },
    error: (err) => {
      this.toastService.showError(err?.error || 'Failed to delete tenant. Please try again.');
    }
  });
}

//tenant action helpers end

//pagination helpers start
get startItem(): number {
  return this.totalCount === 0
    ? 0
    : (this.currentPage - 1) * this.pageSize + 1;
}

get endItem(): number {
  return Math.min(
    this.currentPage * this.pageSize,
    this.totalCount
  );
}
//pagination helpers end
}
