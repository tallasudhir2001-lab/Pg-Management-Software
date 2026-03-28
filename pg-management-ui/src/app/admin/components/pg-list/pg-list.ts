import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Adminservice } from '../../services/adminservice';

interface PgListItem {
  pgId: string;
  name: string;
  address: string;
  contactNumber: string;
  ownerName: string;
  ownerEmail: string;
  userCount: number;
}

@Component({
  selector: 'app-pg-list',
  standalone: true,
  imports: [CommonModule],
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
}
