import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdvanceHistory } from './advance-history';

describe('AdvanceHistory', () => {
  let component: AdvanceHistory;
  let fixture: ComponentFixture<AdvanceHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdvanceHistory]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdvanceHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
