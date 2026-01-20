import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PgSelect } from './pg-select';

describe('PgSelect', () => {
  let component: PgSelect;
  let fixture: ComponentFixture<PgSelect>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PgSelect]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PgSelect);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
