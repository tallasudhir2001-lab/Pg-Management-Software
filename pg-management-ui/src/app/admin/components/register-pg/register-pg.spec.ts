import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterPg } from './register-pg';

describe('RegisterPg', () => {
  let component: RegisterPg;
  let fixture: ComponentFixture<RegisterPg>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterPg]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterPg);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
