// toast.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ToastModel } from '../models/toast.model';

@Injectable({ providedIn: 'root' })
export class ToastService {

  private toastSubject = new BehaviorSubject<ToastModel | null>(null);
  toast$ = this.toastSubject.asObservable();

  showSuccess(message: string) {
    this.show({ message, type: 'success' });
  }

  showError(message: string) {
    this.show({ message, type: 'error' });
  }

  private show(toast: ToastModel) {
    this.toastSubject.next(toast);

    // auto-dismiss after 3 seconds
    setTimeout(() => this.toastSubject.next(null), 3000);
  }
}
