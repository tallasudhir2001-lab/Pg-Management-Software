import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private permissions = new Set<string>();

  constructor() {
    // Restore permissions on page reload if a token already exists in storage
    const token = localStorage.getItem('token');
    if (token) this.loadFromToken(token);
  }

  loadFromToken(token: string): void {
    this.permissions.clear();
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const raw = payload['permissions'];
      if (raw) {
        const keys: string[] = JSON.parse(raw);
        keys.forEach(k => this.permissions.add(k));
      }
    } catch {
      // invalid token — leave permissions empty
    }
  }

  hasAccess(key: string): boolean {
    return this.permissions.has(key);
  }

  hasAnyAccess(keys: string[]): boolean {
    return keys.some(k => this.permissions.has(k));
  }

  hasModuleAccess(module: string): boolean {
    const prefix = module + '.';
    for (const key of this.permissions) {
      if (key.startsWith(prefix)) return true;
    }
    return false;
  }

  clear(): void {
    this.permissions.clear();
  }
}
