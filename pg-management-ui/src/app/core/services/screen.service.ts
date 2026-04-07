import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ScreenService {
  private _isMobile = signal(this.checkMobile());
  readonly isMobile = this._isMobile.asReadonly();

  constructor() {
    window.addEventListener('resize', () => {
      this._isMobile.set(this.checkMobile());
    });
  }

  private checkMobile(): boolean {
    return window.innerWidth < 768;
  }
}
