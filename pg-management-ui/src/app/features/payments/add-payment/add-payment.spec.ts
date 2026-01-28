import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddPayment } from './add-payment';

describe('AddPayment', () => {
  let component: AddPayment;
  let fixture: ComponentFixture<AddPayment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddPayment]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddPayment);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
