import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OccupancyDonutChart } from './occupancy-donut-chart';

describe('OccupancyDonutChart', () => {
  let component: OccupancyDonutChart;
  let fixture: ComponentFixture<OccupancyDonutChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OccupancyDonutChart]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OccupancyDonutChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
