import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenueLineChart } from './charts/revenue-line-chart/revenue-line-chart';
import { OccupancyDonutChart } from './charts/occupancy-donut-chart/occupancy-donut-chart';

@NgModule({
  declarations: [
    // Leave this empty (or for non-standalone components)
  ],
  imports: [
    CommonModule,
    RevenueLineChart,   // Moved here
    OccupancyDonutChart // Moved here
  ],
  exports: [
    RevenueLineChart,
    OccupancyDonutChart
  ]
})
export class SharedModule {}