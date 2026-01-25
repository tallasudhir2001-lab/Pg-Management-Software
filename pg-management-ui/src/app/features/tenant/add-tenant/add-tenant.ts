import { Component, OnInit } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Room } from '../../rooms/models/room.model';
import { FormBuilder,FormGroup,ReactiveFormsModule,Validators } from '@angular/forms';
import { Roomservice } from '../../rooms/services/roomservice';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Tenantservice } from '../services/tenantservice';

@Component({
  selector: 'app-add-tenant',
  standalone :true,
  imports: [ReactiveFormsModule,CommonModule],
  templateUrl: './add-tenant.html',
  styleUrl: '../styles/tenant-form.css',
})
export class AddTenant implements OnInit {
  form!: FormGroup;
  constructor(
    private fb: FormBuilder,
    private roomService: Roomservice,
    private router: Router,
    private tenantService: Tenantservice
  ) {}

  

  // 🔹 Rooms for dropdown
  rooms$!: Observable<Room[]>;
  
  ngOnInit(): void {
    this.form = this.fb.group({
    name: ['', Validators.required],
    contactNumber: ['', Validators.required],
    aadharNumber: [''],
    roomId: ['', Validators.required],   // 👈 GUID goes here
    fromDate: [null],
    advanceAmount: [null],
    rentPaidUpto: [null],
    notes: ['']
  });
    this.loadAvailableRooms();
  }
  private loadAvailableRooms(): void {
    this.rooms$ = this.roomService.getRooms({
      page: 1,
      pageSize: 100,
      status: 'available'
    }).pipe(
      map(res => res.items)
    );
  }
  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.value;

    this.tenantService.createTenant(payload).subscribe({
    next: () => {
      // Navigate back to tenant list
      this.router.navigate(['/tenant-list']);
    },
    error: err => {
      alert(err.error || 'Failed to save tenant');
    }
  });
  }
  cancel(): void {
    this.router.navigate(['/tenant-list']);
  }
}
