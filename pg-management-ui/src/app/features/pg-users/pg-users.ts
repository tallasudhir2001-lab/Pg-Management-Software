import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../shared/toast/toast-service';

interface AssignedPgDto {
  pgId: string;
  pgName: string;
}

interface PgUserDto {
  userId: string;
  name: string;
  email: string;
  role: string;
  assignedPgs: AssignedPgDto[];
}

interface BranchPgDto {
  pgId: string;
  name: string;
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
  branchPgs: BranchPgDto[] = [];
  loading = true;

  showAddForm = false;
  saving = false;

  newUser = { email: '', name: '', password: '', roleName: 'Staff', pgIds: [] as string[] };

  roles = ['Owner', 'Manager', 'Staff'];

  // Inline role editing
  editingRoleUserId: string | null = null;
  editingRole = '';

  // PG assignment editing
  editingPgsUserId: string | null = null;
  editingPgIds: string[] = [];

  // Remove confirm
  pendingRemoveUserId: string | null = null;

  constructor(private http: HttpClient, private toast: ToastService, private cdr: ChangeDetectorRef, private router: Router) {}

  goBack(): void { this.router.navigate(['/settings']); }

  ngOnInit(): void {
    this.loadBranchPgs();
    this.loadUsers();
  }

  loadBranchPgs(): void {
    this.http.get<BranchPgDto[]>(`${this.apiUrl}/pgs`).subscribe({
      next: pgs => { this.branchPgs = pgs; this.cdr.detectChanges(); },
      error: () => {}
    });
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
    const pgIds = this.newUser.pgIds.length ? this.newUser.pgIds : [];
    this.saving = true;
    this.http.post<PgUserDto>(this.apiUrl, { ...this.newUser, pgIds }).subscribe({
      next: user => {
        // Update or add in list
        const idx = this.users.findIndex(u => u.userId === user.userId);
        if (idx >= 0) this.users[idx] = user;
        else this.users.push(user);
        this.newUser = { email: '', name: '', password: '', roleName: 'Staff', pgIds: [] };
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

  // ── Role editing ──────────────────────────────────────────────────────────

  startEditRole(user: PgUserDto): void {
    this.editingRoleUserId = user.userId;
    this.editingRole = user.role;
    this.editingPgsUserId = null;
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

  cancelEditRole(): void { this.editingRoleUserId = null; }

  // ── PG assignment editing ─────────────────────────────────────────────────

  startEditPgs(user: PgUserDto): void {
    this.editingPgsUserId = user.userId;
    this.editingPgIds = user.assignedPgs.map(p => p.pgId);
    this.editingRoleUserId = null;
  }

  isPgSelected(pgId: string): boolean {
    return this.editingPgIds.includes(pgId);
  }

  togglePg(pgId: string): void {
    const idx = this.editingPgIds.indexOf(pgId);
    if (idx >= 0) this.editingPgIds.splice(idx, 1);
    else this.editingPgIds.push(pgId);
  }

  savePgAssignments(user: PgUserDto): void {
    if (!this.editingPgIds.length) {
      this.toast.showError('User must be assigned to at least one PG.');
      return;
    }
    this.http.put(`${this.apiUrl}/${user.userId}/pgs`, { pgIds: this.editingPgIds }).subscribe({
      next: () => {
        user.assignedPgs = this.branchPgs
          .filter(p => this.editingPgIds.includes(p.pgId))
          .map(p => ({ pgId: p.pgId, pgName: p.name }));
        this.editingPgsUserId = null;
        this.cdr.detectChanges();
        this.toast.showSuccess('PG assignments updated.');
      },
      error: () => this.toast.showError('Failed to update PG assignments.')
    });
  }

  cancelEditPgs(): void { this.editingPgsUserId = null; }

  // ── Remove ────────────────────────────────────────────────────────────────

  confirmRemove(userId: string): void { this.pendingRemoveUserId = userId; }

  removeUser(): void {
    if (!this.pendingRemoveUserId) return;
    const userId = this.pendingRemoveUserId;
    this.pendingRemoveUserId = null;
    this.http.delete(`${this.apiUrl}/${userId}`).subscribe({
      next: () => {
        this.users = this.users.filter(u => u.userId !== userId);
        this.cdr.detectChanges();
        this.toast.showSuccess('User removed from branch.');
      },
      error: () => this.toast.showError('Failed to remove user.')
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  toggleNewUserPg(pgId: string): void {
    const idx = this.newUser.pgIds.indexOf(pgId);
    if (idx >= 0) this.newUser.pgIds.splice(idx, 1);
    else this.newUser.pgIds.push(pgId);
  }

  isNewUserPgSelected(pgId: string): boolean {
    return this.newUser.pgIds.includes(pgId);
  }

  roleBadgeClass(role: string): string {
    switch (role) {
      case 'Owner': return 'badge-owner';
      case 'Manager': return 'badge-manager';
      default: return 'badge-staff';
    }
  }
}
