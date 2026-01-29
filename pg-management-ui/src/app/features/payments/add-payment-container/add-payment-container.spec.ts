import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddPaymentContainer } from './add-payment-container';

describe('AddPaymentContainer', () => {
  let component: AddPaymentContainer;
  let fixture: ComponentFixture<AddPaymentContainer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddPaymentContainer]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddPaymentContainer);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
