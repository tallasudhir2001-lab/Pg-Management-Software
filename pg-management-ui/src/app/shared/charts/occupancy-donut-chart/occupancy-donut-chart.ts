import {
  Component,
  Input,
  ElementRef,
  ViewChild,
  AfterViewInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy
} from '@angular/core';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  registerables,
  ChartOptions
} from 'chart.js';
Chart.register(...registerables);

@Component({
  selector: 'app-occupancy-donut-chart',
  imports: [],
  templateUrl: './occupancy-donut-chart.html',
  styleUrl: './occupancy-donut-chart.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OccupancyDonutChart
  implements AfterViewInit, OnChanges {

  @Input() occupied = 0;
  @Input() vacant = 0;

  @ViewChild('canvas', { static: true })
  canvas!: ElementRef<HTMLCanvasElement>;

  private chart!: Chart;

  ngAfterViewInit(): void {
    this.createChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.chart) {
      this.updateChart();
    }
  }

  private createChart(): void {

  const options: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '70%',   // ✅ now TypeScript is happy
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  const config: ChartConfiguration<'doughnut'> = {
    type: 'doughnut',
    data: {
      labels: ['Occupied', 'Vacant'],
      datasets: [{
        data: [this.occupied, this.vacant],
        backgroundColor: [
          '#0F1041', // Your primary theme color (Occupied)
          '#E0E0E5'  // A neutral light grey (Vacant)
        ],
        hoverBackgroundColor: [
          '#1A1B5E', 
          '#D1D1D6'
        ],
        borderWidth: 0
      }]
    },
    options
  };

  this.chart = new Chart(
    this.canvas.nativeElement,
    config
  );
}

  private updateChart(): void {
    this.chart.data.datasets[0].data = [
      this.occupied,
      this.vacant
    ];
    this.chart.update();
  }
}