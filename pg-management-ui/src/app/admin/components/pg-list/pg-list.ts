import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Adminservice } from '../../services/adminservice';
import { ToastService } from '../../../shared/toast/toast-service';

interface PgListItem {
  pgId: string;
  name: string;
  address: string;
  contactNumber: string;
  ownerName: string;
  ownerEmail: string;
  userCount: number;
  branchId: string | null;
  branchName: string | null;
  isEmailSubscriptionEnabled: boolean;
  isWhatsappSubscriptionEnabled: boolean;
}

@Component({
  selector: 'app-pg-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pg-list.html',
  styleUrl: './pg-list.css',
})
export class AdminPgList implements OnInit {
  pgs: PgListItem[] = [];
  loading = true;
  error = '';

  editingPg: PgListItem | null = null;
  editForm = { name: '', address: '', contactNumber: '', branchName: '', ownerEmail: '' };
  saving = false;

  constructor(
    private adminService: Adminservice,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPgs();
  }

  loadPgs(): void {
    this.loading = true;
    this.adminService.getPgs().subscribe({
      next: pgs => { this.pgs = pgs; this.loading = false; this.cdr.detectChanges(); },
      error: () => { this.error = 'Failed to load PGs.'; this.loading = false; this.cdr.detectChanges(); }
    });
  }

  openEdit(pg: PgListItem): void {
    this.editingPg = pg;
    this.editForm = {
      name: pg.name,
      address: pg.address,
      contactNumber: pg.contactNumber,
      branchName: pg.branchName || '',
      ownerEmail: pg.ownerEmail || ''
    };
  }

  closeEdit(): void {
    this.editingPg = null;
  }

  saveEdit(): void {
    if (this.saving || !this.editingPg) return;
    this.saving = true;
    const pg = this.editingPg;

    this.adminService.updatePgDetails(pg.pgId, this.editForm).subscribe({
      next: () => {
        pg.name = this.editForm.name;
        pg.address = this.editForm.address;
        pg.contactNumber = this.editForm.contactNumber;
        pg.branchName = this.editForm.branchName || pg.branchName;
        pg.ownerEmail = this.editForm.ownerEmail || pg.ownerEmail;
        this.editingPg = null;
        this.saving = false;
        this.toast.showSuccess('PG details updated.');
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.saving = false;
        this.toast.showError(err?.error || 'Failed to update PG details.');
        this.cdr.detectChanges();
      }
    });
  }

  toggleSubscription(pg: PgListItem, type: 'email' | 'whatsapp'): void {
    const updated = {
      isEmailSubscriptionEnabled: type === 'email' ? !pg.isEmailSubscriptionEnabled : pg.isEmailSubscriptionEnabled,
      isWhatsappSubscriptionEnabled: type === 'whatsapp' ? !pg.isWhatsappSubscriptionEnabled : pg.isWhatsappSubscriptionEnabled
    };

    this.adminService.updatePgSubscription(pg.pgId, updated).subscribe({
      next: () => {
        pg.isEmailSubscriptionEnabled = updated.isEmailSubscriptionEnabled;
        pg.isWhatsappSubscriptionEnabled = updated.isWhatsappSubscriptionEnabled;
        this.cdr.detectChanges();
      },
      error: () => { this.error = 'Failed to update subscription.'; this.cdr.detectChanges(); }
    });
  }
}
