import { Component, inject } from '@angular/core';
import { ScreenService } from '../../core/services/screen.service';
import { RoomDetails } from './room-details/room-details';
import { MobileRoomDetails } from './mobile-room-details/mobile-room-details';

@Component({
  selector: 'app-room-details-page',
  standalone: true,
  imports: [RoomDetails, MobileRoomDetails],
  template: `
    @if (screen.isMobile()) {
      <app-mobile-room-details />
    } @else {
      <app-room-details />
    }
  `
})
export class RoomDetailsPage {
  screen = inject(ScreenService);
}
