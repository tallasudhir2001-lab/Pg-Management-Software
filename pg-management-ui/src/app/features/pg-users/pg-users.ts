import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../shared/toast/toast-service';

interface PgUserDto {
  userId: string;
  name: string;
  email: string;
  role: string;
}

@Component({
  selector: 'app-pg-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pg-users.html',
  styleUrl: './pg-users.css',
})
export class PgUserManagement implements OnInit {
  private apiUrl = `${environment.apiBaseUrl}/pg-users`;

  users: PgUserDto[] = [];
  loading = true;

  showAddForm = false;
  saving = false;

  newUser = { email: '', name: '', password: '', roleName: 'Staff' };

  roles = ['Owner', 'Manager', 'Staff'];

  // Inline role editing
  editingRoleUserId: string | null = null;
  editingRole = '';

  // Remove confirm
  pendingRemoveUserId: string | null = null;

  constructor(private http: HttpClient, private toast: ToastService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.http.get<PgUserDto[]>(this.apiUrl).subscribe({
      next: users => { this.users = users; this.loading = false; this.cdr.detectChanges(); },
      error: () => { this.loading = false; this.cdr.detectChanges(); this.toast.showError('Failed to load users.'); }
    });
  }

  addUser(): void {
    if (this.saving) return;
    this.saving = true;
    this.http.post<PgUserDto>(this.apiUrl, this.newUser).subscribe({
      next: user => {
        this.users.push(user);
        this.newUser = { email: '', name: '', password: '', roleName: 'Staff' };
        this.showAddForm = false;
        this.saving = false;
        this.cdr.detectChanges();
        this.toast.showSuccess('User added successfully.');
      },
      error: err => {
        this.saving = false;
        this.cdr.detectChanges();
        this.toast.showError(err.error || 'Failed to add user.');
      }
    });
  }

  startEditRole(user: PgUserDto): void {
    this.editingRoleUserId = user.userId;
    this.editingRole = user.role;
  }

  saveRole(user: PgUserDto): void {
    this.http.put(`${this.apiUrl}/${user.userId}/role`, { roleName: this.editingRole }).subscribe({
      next: () => {
        user.role = this.editingRole;
        this.editingRoleUserId = null;
        this.cdr.detectChanges();
        this.toast.showSuccess('Role updated.');
      },
      error: () => this.toast.showError('Failed to update role.')
    });
  }

  cancelEditRole(): void {
    this.editingRoleUserId = null;
  }

  confirmRemove(userId: string): void {
    this.pendingRemoveUserId = userId;
  }

  removeUser(): void {
    if (!this.pendingRemoveUserId) return;
    const userId = this.pendingRemoveUserId;
    this.pendingRemoveUserId = null;
    this.http.delete(`${this.apiUrl}/${userId}`).subscribe({
      next: () => {
        this.users = this.users.filter(u => u.userId !== userId);
        this.cdr.detectChanges();
        this.toast.showSuccess('User removed from PG.');
      },
      error: () => this.toast.showError('Failed to remove user.')
    });
  }

  roleBadgeClass(role: string): string {
    switch (role) {
      case 'Owner': return 'badge-owner';
      case 'Manager': return 'badge-manager';
      default: return 'badge-staff';
    }
  }
}
