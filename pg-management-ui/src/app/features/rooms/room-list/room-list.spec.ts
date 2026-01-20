import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RoomList } from './room-list';

describe('RoomList', () => {
  let component: RoomList;
  let fixture: ComponentFixture<RoomList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoomList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RoomList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
