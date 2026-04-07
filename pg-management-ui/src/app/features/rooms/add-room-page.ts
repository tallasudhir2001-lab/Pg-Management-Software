import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { AddRoom } from './add-room/add-room';
import { MobileAddRoom } from './mobile-add-room/mobile-add-room';

@Component({
  selector: 'app-add-room-page',
  standalone: true,
  imports: [AddRoom, MobileAddRoom],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-add-room />
    } @else {
      <app-add-room />
    }
  `
})
export class AddRoomPage {
  screen = inject(ScreenService);
}
