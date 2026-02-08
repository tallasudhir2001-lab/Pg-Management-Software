import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExpenseDrawer } from './expense-drawer';

describe('AddExpense', () => {
  let component: ExpenseDrawer;
  let fixture: ComponentFixture<ExpenseDrawer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpenseDrawer]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExpenseDrawer);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
