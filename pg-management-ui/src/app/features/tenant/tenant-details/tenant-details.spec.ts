import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TenantDetails } from './tenant-details';

describe('TenantDetails', () => {
  let component: TenantDetails;
  let fixture: ComponentFixture<TenantDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TenantDetails]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TenantDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
