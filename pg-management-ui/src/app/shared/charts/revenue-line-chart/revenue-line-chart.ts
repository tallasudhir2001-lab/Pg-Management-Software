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
  registerables
} from 'chart.js';
import { RevenueTrend } from '../../../features/dashboard/models/revenue-trend.model';

@Component({
  selector: 'app-revenue-line-chart',
  imports: [],
  templateUrl: './revenue-line-chart.html',
  styleUrl: './revenue-line-chart.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class RevenueLineChart
  implements AfterViewInit, OnChanges {

  @Input() data: RevenueTrend[] = [];

  @ViewChild('canvas', { static: true })
  canvas!: ElementRef<HTMLCanvasElement>;

  private chart!: Chart;

  ngAfterViewInit(): void {
    if (this.data.length) {
      this.createChart();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data'] && this.chart) {
      this.updateChart();
    }
  }

  private createChart(): void {
    const ctx = this.canvas.nativeElement.getContext('2d');
    
    // Create a subtle gradient for the 'fill' area
    let gradient = null;
    if (ctx) {
      gradient = ctx.createLinearGradient(0, 0, 0, 260);
      gradient.addColorStop(0, 'rgba(15, 16, 65, 0.2)'); // Theme color with 20% opacity
      gradient.addColorStop(1, 'rgba(15, 16, 65, 0)');   // Fades to transparent
    }
  const config: ChartConfiguration<'line'> = {
    type: 'line',
    data: {
      labels: this.getLabelsFromData(),
      datasets: [{
        label: 'Revenue',
        data: this.data.map(d => d.amount),
        borderColor: '#0F1041',       
          backgroundColor: gradient || '#0F1041', 
          pointBackgroundColor: '#0F1041',
          pointHoverRadius: 6,
          pointRadius: 4,
        borderWidth: 3,
        tension: 0.4,
        fill: true
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
            backgroundColor: '#0F1041',
            titleColor: '#fff',
            bodyColor: '#fff',
            displayColors: false
          }
      },
      scales: {
        y: {
          ticks: {
            callback: value => `₹${value}`,
            color: '#0F1041' 
          },
          grid:{
            color: 'rgba(0, 0, 0, 0.05)'
          }
        }
      }
    }
  };

  this.chart = new Chart(this.canvas.nativeElement, config);
}


 private updateChart(): void {
  this.chart.data.labels = this.getLabelsFromData();
  this.chart.data.datasets[0].data = this.data.map(d => d.amount);
  this.chart.update();
}


private getLabelsFromData(): string[] {
  return this.data.map(d =>
    new Date(2000, d.month - 1, 1).toLocaleString('default', {
      month: 'short'
    })
  );
}

}
