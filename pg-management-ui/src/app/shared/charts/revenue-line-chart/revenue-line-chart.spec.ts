import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RevenueLineChart } from './revenue-line-chart';

describe('RevenueLineChart', () => {
  let component: RevenueLineChart;
  let fixture: ComponentFixture<RevenueLineChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RevenueLineChart]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RevenueLineChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
