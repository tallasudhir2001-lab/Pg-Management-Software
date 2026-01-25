import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantListDto } from '../models/tenant-list-dto';
import { ActivatedRoute, Router } from '@angular/router';
import { Tenantservice } from '../services/tenantservice';
import { TenantDetailsModel } from '../models/tenant-details.model';
import { Observable, startWith, Subject, switchMap, tap } from 'rxjs';
import { UpdateTenantDto } from '../models/update-tenant-dto';

@Component({
  selector: 'app-tenant-details',
  standalone : true,
  imports: [CommonModule,FormsModule],
  templateUrl: './tenant-details.html',
  styleUrl: '../styles/tenant-form.css',
})
export class TenantDetails implements OnInit{
  tenantId!:string;
  mode: 'view' | 'edit' = 'view';

  tenant$!: Observable<TenantDetailsModel>;
  //reload trigger
  private reload$ = new Subject<void>();
  editableTenant!: TenantDetailsModel;
  isLoading = true;
  isSaving = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tenantService: Tenantservice
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
    })
  );
}


//remove later these two methods
  enableEdit(): void {
    this.router.navigate(['/tenants', this.tenantId, 'edit']);
  }

  cancelEdit(): void {
    this.router.navigate(['/tenants', this.tenantId]);
  }
  // save(): void {
  //   this.isSaving = true;

  //   this.tenantService.updateTenant(this.tenantId, this.tenant).subscribe({
  //     next: () => {
  //       this.isSaving = false;
  //       this.router.navigate(['/tenants', this.tenantId]);
  //     }
  //   });
  // }
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

  }

  confirmMoveOut(): void {
  const confirmed = confirm(
    'Are you sure you want to move out this tenant? This action cannot be undone.'
  );

  if (!confirmed) return;

  this.tenantService.moveOutTenant(this.tenantId).subscribe({
    next: () => {
      
      this.mode = 'view';
      this.reload$.next();

      // Ensure we are in view mode
    },
    error: err => {
      alert(err?.error || 'Failed to move out tenant');
    }
  });
}

//action icons methods end

  save(): void {
  if (this.mode !== 'edit') return;

  const dto = this.mapToUpdateDto(this.editableTenant);

  this.tenantService.updateTenant(this.tenantId, dto).subscribe({
    next: () => {
      // reload fresh data from backend
      this.tenant$ = this.tenantService.getTenantById(this.tenantId).pipe(
        tap(t => (this.editableTenant = { ...t }))
      );
      //to stay on the same page after save in view mode.
      //this.mode = 'view';

      //for now lets go back to tenant list
      this.router.navigate(['/tenant-list']);
    },
    error: () => {
      alert('Failed to save tenant details');
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
    advanceAmount: tenant.advanceAmount,
    rentPaidUpto: tenant.rentPaidUpto,
    notes: tenant.notes
  };
}


}
