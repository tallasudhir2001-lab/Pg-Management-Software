import { Component, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmDialogService } from './confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  encapsulation: ViewEncapsulation.None,
  template: `
    <ng-container *ngIf="confirmService.state$ | async as state">
      <div class="cd-overlay" (click)="confirmService.close(false)">
        <div class="cd-dialog" (click)="$event.stopPropagation()">
          <div class="cd-header">
            <svg class="cd-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="12" y1="8" x2="12" y2="12"/>
              <line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            <h3 class="cd-title">{{ state.data.title }}</h3>
          </div>
          <p class="cd-message">{{ state.data.message }}</p>
          <div class="cd-actions">
            <button class="cd-btn cd-btn-cancel" (click)="confirmService.close(false)">{{ state.data.cancelText || 'Cancel' }}</button>
            <button class="cd-btn cd-btn-confirm" (click)="confirmService.close(true)">{{ state.data.confirmText || 'Delete' }}</button>
          </div>
        </div>
      </div>
    </ng-container>
  `,
  styles: [`
    .cd-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
      animation: cd-fade-in 0.15s ease-out;
    }
    .cd-dialog {
      background: #fff;
      border-radius: 12px;
      padding: 24px;
      width: 90%;
      max-width: 400px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
      animation: cd-slide-up 0.2s ease-out;
    }
    .cd-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 12px;
    }
    .cd-icon {
      width: 28px;
      height: 28px;
      color: #dc2626;
      flex-shrink: 0;
    }
    .cd-title {
      margin: 0;
      font-size: 17px;
      font-weight: 600;
      color: #111827;
    }
    .cd-message {
      margin: 0 0 20px;
      font-size: 14px;
      color: #6b7280;
      line-height: 1.5;
      padding-left: 40px;
    }
    .cd-actions {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
    }
    .cd-btn {
      padding: 9px 20px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      border: none;
      transition: background 0.15s, transform 0.1s;
    }
    .cd-btn:active { transform: scale(0.97); }
    .cd-btn-cancel {
      background: #f3f4f6;
      color: #374151;
    }
    .cd-btn-cancel:hover { background: #e5e7eb; }
    .cd-btn-confirm {
      background: #dc2626;
      color: #fff;
    }
    .cd-btn-confirm:hover { background: #b91c1c; }

    @keyframes cd-fade-in {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes cd-slide-up {
      from { opacity: 0; transform: translateY(10px) scale(0.97); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }
  `]
})
export class ConfirmDialog {
  constructor(public confirmService: ConfirmDialogService) {}
}
