import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Adminservice } from '../../services/adminservice';

interface PgListItem {
  pgId: string;
  name: string;
  address: string;
  contactNumber: string;
  ownerName: string;
  ownerEmail: string;
  userCount: number;
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

  constructor(private adminService: Adminservice, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.adminService.getPgs().subscribe({
      next: pgs => { this.pgs = pgs; this.loading = false; this.cdr.detectChanges(); },
      error: () => { this.error = 'Failed to load PGs.'; this.loading = false; this.cdr.detectChanges(); }
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
