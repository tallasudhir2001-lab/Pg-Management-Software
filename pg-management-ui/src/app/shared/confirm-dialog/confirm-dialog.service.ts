import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
}

export interface ConfirmDialogState {
  visible: boolean;
  data: ConfirmDialogData;
  resolve: (result: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private stateSubject = new BehaviorSubject<ConfirmDialogState | null>(null);
  state$ = this.stateSubject.asObservable();

  confirm(data: ConfirmDialogData): Promise<boolean> {
    return new Promise(resolve => {
      this.stateSubject.next({ visible: true, data, resolve });
    });
  }

  close(result: boolean): void {
    const current = this.stateSubject.value;
    if (current) {
      this.stateSubject.next(null);
      current.resolve(result);
    }
  }
}
