import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Adminservice } from '../../services/adminservice';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-register-pg',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './register-pg.html',
  styleUrl: './register-pg.css',
})
export class RegisterPg {
  pgName = '';
  address = '';
  contactNumber = '';
  ownerName = '';
  ownerEmail = '';
  password = '';
  saving = false;

  constructor(
    private adminservice: Adminservice,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  get isValid(): boolean {
    return !!(this.pgName && this.address && this.contactNumber && this.ownerName && this.ownerEmail && this.password);
  }

  registerPg() {
    if (!this.isValid || this.saving) return;
    this.saving = true;

    this.adminservice.registerPg({
      pgName: this.pgName,
      address: this.address,
      contactNumber: this.contactNumber,
      ownerName: this.ownerName,
      ownerEmail: this.ownerEmail,
      password: this.password
    }).subscribe({
      next: () => {
        this.saving = false;
        this.pgName = this.address = this.contactNumber = '';
        this.ownerName = this.ownerEmail = this.password = '';
        this.cdr.detectChanges();
        this.toast.showSuccess('PG registered successfully.');
      },
      error: err => {
        this.saving = false;
        this.cdr.detectChanges();
        this.toast.showError(err?.error || 'Failed to register PG.');
      }
    });
  }
}
