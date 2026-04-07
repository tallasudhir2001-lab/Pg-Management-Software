import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { RoomList } from './room-list/room-list';
import { MobileRoomList } from './mobile-room-list/mobile-room-list';

@Component({
  selector: 'app-room-list-page',
  standalone: true,
  imports: [RoomList, MobileRoomList],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-room-list />
    } @else {
      <app-room-list />
    }
  `
})
export class RoomListPage {
  screen = inject(ScreenService);
}
