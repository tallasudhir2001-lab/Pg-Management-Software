import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddTenant } from './add-tenant';

describe('AddTenant', () => {
  let component: AddTenant;
  let fixture: ComponentFixture<AddTenant>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddTenant]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddTenant);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
