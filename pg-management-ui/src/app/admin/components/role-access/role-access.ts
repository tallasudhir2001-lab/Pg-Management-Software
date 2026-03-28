import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ToastService } from '../../../shared/toast/toast-service';

interface AccessPointDto {
  id: number;
  key: string;
  displayName: string;
  httpMethod: string;
  route: string;
}

interface AccessPointModuleDto {
  module: string;
  accessPoints: AccessPointDto[];
}

interface RoleDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-role-access',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './role-access.html',
  styleUrl: './role-access.css',
})
export class RoleAccessComponent implements OnInit {
  private readonly baseUrl = `${environment.apiBaseUrl}/admin/access-points`;

  roles: RoleDto[] = [];
  selectedRoleId: string | null = null;
  modules: AccessPointModuleDto[] = [];
  assignedIds = new Set<number>();
  saving = false;
  loading = false;

  constructor(private http: HttpClient, private toastService: ToastService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.http.get<RoleDto[]>(`${this.baseUrl}/roles`).subscribe({
      next: roles => { this.roles = roles; this.cdr.detectChanges(); },
    });

    this.http.get<AccessPointModuleDto[]>(this.baseUrl).subscribe({
      next: modules => { this.modules = modules; this.cdr.detectChanges(); },
    });
  }

  onRoleChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    this.selectedRoleId = id || null;
    if (!this.selectedRoleId) {
      this.assignedIds.clear();
      return;
    }
    this.loading = true;
    this.http.get<number[]>(`${this.baseUrl}/role/${this.selectedRoleId}`).subscribe({
      next: ids => {
        this.assignedIds = new Set(ids);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  isChecked(id: number): boolean {
    return this.assignedIds.has(id);
  }

  toggle(id: number): void {
    if (this.assignedIds.has(id)) {
      this.assignedIds.delete(id);
    } else {
      this.assignedIds.add(id);
    }
  }

  moduleState(mod: AccessPointModuleDto): 'all' | 'none' | 'partial' {
    const total = mod.accessPoints.length;
    const checked = mod.accessPoints.filter(ap => this.assignedIds.has(ap.id)).length;
    if (checked === 0) return 'none';
    if (checked === total) return 'all';
    return 'partial';
  }

  toggleModule(mod: AccessPointModuleDto): void {
    const state = this.moduleState(mod);
    if (state === 'all') {
      mod.accessPoints.forEach(ap => this.assignedIds.delete(ap.id));
    } else {
      mod.accessPoints.forEach(ap => this.assignedIds.add(ap.id));
    }
  }

  save(): void {
    if (!this.selectedRoleId || this.saving) return;
    this.saving = true;
    this.http.put(`${this.baseUrl}/role/${this.selectedRoleId}`, {
      accessPointIds: Array.from(this.assignedIds)
    }).subscribe({
      next: () => {
        this.saving = false;
        this.cdr.detectChanges();
        this.toastService.showSuccess('Permissions saved successfully.');
      },
      error: () => {
        this.saving = false;
        this.cdr.detectChanges();
        this.toastService.showError('Failed to save permissions.');
      }
    });
  }

  methodClass(method: string): string {
    switch (method.toUpperCase()) {
      case 'GET':    return 'badge-get';
      case 'POST':   return 'badge-post';
      case 'PUT':    return 'badge-put';
      case 'PATCH':  return 'badge-patch';
      case 'DELETE': return 'badge-delete';
      default:       return '';
    }
  }
}
