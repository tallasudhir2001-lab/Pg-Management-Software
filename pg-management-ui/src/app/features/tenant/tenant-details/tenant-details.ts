import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantListDto } from '../models/tenant-list-dto';
import { ActivatedRoute, Router } from '@angular/router';
import { Tenantservice } from '../services/tenantservice';
import { Stay, TenantDetailsModel } from '../models/tenant-details.model';
import { map, Observable, startWith, Subject, switchMap, tap } from 'rxjs';
import { UpdateTenantDto } from '../models/update-tenant-dto';
import { Room } from '../../rooms/models/room.model';
import { Roomservice } from '../../rooms/services/roomservice';
import { PendingRent } from '../models/pending-rent.model';
import { ToastService } from '../../../shared/toast/toast-service';
import { PaymentHistory } from '../payment-history/payment-history';
import { AdvanceHistory } from '../../advances/advance-history/advance-history';

@Component({
  selector: 'app-tenant-details',
  standalone : true,
  imports: [CommonModule,FormsModule,PaymentHistory,AdvanceHistory],
  templateUrl: './tenant-details.html',
  styleUrl: '../styles/tenant-form.css',
})
export class TenantDetails implements OnInit{
  tenantId!:string;
  mode: 'view' | 'edit' = 'view';

  tenant$!: Observable<TenantDetailsModel>;
  currentTenant!: TenantDetailsModel;
  //reload trigger
  reload$ = new Subject<void>();
  editableTenant!: TenantDetailsModel;
  isLoading = true;
  isSaving = false;

  //for change room
  isChangeRoomOpen = false;
  rooms$!: Observable<Room[]>;
  selectedRoomId: string | null = null;
  //loading state
  isChangingRoom = false;
  changeDate: string | null = null;


  //tenant pending Rent Variables
  pendingRent$!: Observable<PendingRent>;
  pendingRentLoading = true;
  pendingRentError = '';

  //stays
  stays: Stay[] = [];

  isCreateStayOpen = false;
  selectedRoomIdForStay: string | null = null;
  createStayDate: string | null = null;
  isCreatingStay = false;

  //move out
  isMoveOutOpen = false;
  hasActiveAdvance = false;



  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tenantService: Tenantservice,
    private roomService: Roomservice,
    private toastService: ToastService
  ) {}
ngOnInit(): void {
  this.tenantId = this.route.snapshot.paramMap.get('id')!;
  this.mode = this.route.snapshot.data['mode'];

  this.tenant$ = this.reload$.pipe(
    startWith(void 0), // initial load
    switchMap(() =>
      this.tenantService.getTenantById(this.tenantId)
    ),
    tap(t => {
      this.editableTenant = { ...t };
      this.stays = t.stays || [];
      this.currentTenant = t; 
    })
  );
 this.pendingRent$ = this.reload$.pipe(
  startWith(void 0),
  switchMap(() =>
    this.tenantService.getPendingRent(this.tenantId)
  ),
  tap(() => this.pendingRentLoading = false)
);
  
}
confirmCreateStay(): void {
  if (!this.selectedRoomIdForStay) return;

  if (!this.createStayDate) {
    this.toastService.showError('Please select start date');
    return;
  }

  this.isCreatingStay = true;

  const payload = {
    tenantId: this.tenantId,
    roomId: this.selectedRoomIdForStay,
    fromDate: this.createStayDate
  };

  this.tenantService.createStay(payload).subscribe({
    next: () => {
      this.isCreatingStay = false;
      this.isCreateStayOpen = false;

      this.toastService.showSuccess('Stay created successfully');

      this.reload$.next();
    },
    error: err => {
      this.isCreatingStay = false;
      this.toastService.showError(err?.error || 'Failed to create stay');
    }
  });
}

openCreateStay(): void {
  this.isCreateStayOpen = true;
  this.selectedRoomIdForStay = null;

  this.createStayDate = new Date().toISOString().split('T')[0];

  this.rooms$ = this.roomService.getRooms({
    page: 1,
    pageSize: 100
  }).pipe(map(res => res.items));
}

closeCreateStay(): void {
  this.isCreateStayOpen = false;
}
closeMoveOut(): void {
  this.isMoveOutOpen = false;
}

proceedMoveOut(): void {
  this.tenantService.moveOutTenant(this.tenantId).subscribe({
    next: () => {
      this.isMoveOutOpen = false;
      this.toastService.showSuccess('Tenant moved out successfully');
      this.reload$.next();
    },
    error: err => {
      this.toastService.showError(err?.error || 'Failed to move out');
    }
  });
}

//remove later these two methods
  enableEdit(): void {
    this.router.navigate(['/tenants', this.tenantId, 'edit']);
  }

  cancelEdit(): void {
    this.router.navigate(['/tenants', this.tenantId]);
  }

  cancel(): void {
    this.router.navigate(['/tenant-list']);
  }
  //action icons methods start
  toggleMode(): void {
    if (this.mode === 'edit') {
      // going back to view → discard changes
      this.editableTenant = { ...this.editableTenant };
      this.mode = 'view';
    } else {
      this.mode = 'edit';
    }
  }

  openChangeRoom(): void {
  this.isChangeRoomOpen = true;
  this.selectedRoomId = null;

  // load available rooms
  this.rooms$ = this.roomService.getRooms({
    page: 1,
    pageSize: 100
  }).pipe(
    map(res => res.items)
  );
}
  closeChangeRoom(): void {
  this.isChangeRoomOpen = false;
}
confirmChangeRoom(): void {
  if (!this.selectedRoomId) return;

  if (!this.changeDate) {
    this.toastService.showError('Please select change date');
    return;
  }

  this.isChangingRoom = true;

  const payload = {
    newRoomId: this.selectedRoomId,
    changeDate: this.changeDate
  };

  this.tenantService
    .changeRoom(this.tenantId, payload)
    .subscribe({
      next: () => {
        this.isChangingRoom = false;
        this.isChangeRoomOpen = false;

        // reload tenant details (same mechanism)
        this.reload$.next();
        this.toastService.showError('room change successful');
      },
      error: err => {
        this.isChangingRoom = false;
        this.toastService.showError(err?.error || 'Failed to change room');
      }
    });
}

 confirmMoveOut(): void {
  this.hasActiveAdvance = !!this.currentTenant?.advanceAmount;
  this.isMoveOutOpen = true;
}


//action icons methods end

  save(): void {
  if (this.mode !== 'edit') return;

  const dto = this.mapToUpdateDto(this.editableTenant);

  this.tenantService.updateTenant(this.tenantId, dto).subscribe({
    next: () => {
      // reload fresh data from backend
      this.toastService.showSuccess('Tenant updated Successfully.');
      this.tenant$ = this.tenantService.getTenantById(this.tenantId).pipe(
        tap(t => (this.editableTenant = { ...t }))
      );
      //to stay on the same page after save in view mode.
      //this.mode = 'view';

      //for now lets go back to tenant list
      this.router.navigate(['/tenant-list']);
    },
    error: (err) => {
      this.toastService.showError(err?.error || 'Failed to Update Tenant');
    }
  });
}

//helper method to transform tenantdetailModel to UpdateTenantDto
private mapToUpdateDto(
  tenant: TenantDetailsModel
): UpdateTenantDto {
  return {
    name: tenant.name,
    contactNumber: tenant.contactNumber,
    aadharNumber: tenant.aadharNumber,
    notes: tenant.notes
  };
}

// Payments methods start
  addPayment(): void {
    this.router.navigate([
      '/payments/add',
      this.tenantId
    ]);
  }
//payment methods end
}
