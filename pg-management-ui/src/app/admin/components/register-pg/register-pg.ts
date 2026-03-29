import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Adminservice, BranchDto } from '../../services/adminservice';
import { ToastService } from '../../../shared/toast/toast-service';

@Component({
  selector: 'app-register-pg',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './register-pg.html',
  styleUrl: './register-pg.css',
})
export class RegisterPg implements OnInit {
  pgName = '';
  address = '';
  contactNumber = '';
  ownerName = '';
  ownerEmail = '';
  password = '';
  saving = false;

  // Branch selection
  branchMode: 'new' | 'existing' = 'new';
  branches: BranchDto[] = [];
  selectedBranchId = '';
  loadingBranches = false;

  constructor(
    private adminservice: Adminservice,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadBranches();
  }

  loadBranches(): void {
    this.loadingBranches = true;
    this.adminservice.getBranches().subscribe({
      next: branches => {
        this.branches = branches;
        this.loadingBranches = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingBranches = false;
        this.cdr.detectChanges();
      }
    });
  }

  get isExistingBranch(): boolean {
    return this.branchMode === 'existing';
  }

  get isValid(): boolean {
    const pgValid = !!(this.pgName && this.address && this.contactNumber);
    if (this.isExistingBranch) {
      return pgValid && !!this.selectedBranchId;
    }
    return pgValid && !!(this.ownerName && this.ownerEmail && this.password);
  }

  registerPg() {
    if (!this.isValid || this.saving) return;
    this.saving = true;

    const payload: any = {
      pgName: this.pgName,
      address: this.address,
      contactNumber: this.contactNumber,
    };

    if (this.isExistingBranch) {
      payload.branchId = this.selectedBranchId;
      // Owner is derived from existing branch — no owner fields needed
      payload.ownerName = '';
      payload.ownerEmail = '';
      payload.password = '';
    } else {
      payload.ownerName = this.ownerName;
      payload.ownerEmail = this.ownerEmail;
      payload.password = this.password;
    }

    this.adminservice.registerPg(payload).subscribe({
      next: () => {
        this.saving = false;
        this.pgName = this.address = this.contactNumber = '';
        this.ownerName = this.ownerEmail = this.password = '';
        this.selectedBranchId = '';
        this.branchMode = 'new';
        this.cdr.detectChanges();
        this.toast.showSuccess('PG registered successfully.');
        this.loadBranches(); // refresh branch list
      },
      error: err => {
        this.saving = false;
        this.cdr.detectChanges();
        this.toast.showError(err?.error || 'Failed to register PG.');
      }
    });
  }
}
