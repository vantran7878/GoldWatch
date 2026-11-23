import {
  Component, Input, OnInit, OnChanges, SimpleChanges,
  ViewChild, ElementRef, AfterViewInit, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { GoldPrice } from '../../core/models/gold-price.model';
import {
  Chart, LineController, LineElement, PointElement,
  LinearScale, CategoryScale, Tooltip, Legend, Filler
} from 'chart.js';

// Chỉ đăng ký các module cần thiết (tree-shake)
Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend, Filler);

@Component({
  selector: 'app-price-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './price-chart.component.html',
  styleUrl: './price-chart.component.scss'
})
export class PriceChartComponent implements OnInit, OnChanges, AfterViewInit, OnDestroy {
  @Input() prices: GoldPrice[] = [];
  @ViewChild('chartCanvas') chartCanvasRef!: ElementRef<HTMLCanvasElement>;

  private chart: Chart | null = null;
  private isViewReady = false;

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    this.isViewReady = true;
    this.buildChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['prices'] && this.isViewReady) {
      this.buildChart();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private buildChart(): void {
    if (!this.chartCanvasRef) return;

    // Lấy 30 bản ghi gần nhất, sort từ cũ → mới
    const data = [...this.prices]
      .sort((a, b) => new Date(a.collectedAt).getTime() - new Date(b.collectedAt).getTime())
      .slice(-30);

    const labels = data.map(p =>
      new Date(p.collectedAt).toLocaleString('vi-VN', {
        month: 'short', day: 'numeric',
        hour: '2-digit', minute: '2-digit'
      })
    );

    const buyData  = data.map(p => p.buyPrice);
    const sellData = data.map(p => p.sellPrice);

    if (this.chart) {
      // Cập nhật data thay vì tạo lại chart
      this.chart.data.labels = labels;
      this.chart.data.datasets[0].data = sellData;
      this.chart.data.datasets[1].data = buyData;
      this.chart.update('active');
      return;
    }

    const ctx = this.chartCanvasRef.nativeElement.getContext('2d')!;

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Giá Bán',
            data: sellData,
            borderColor: '#f59e0b',
            backgroundColor: 'rgba(245, 158, 11, 0.08)',
            fill: true,
            tension: 0.3,
            pointRadius: data.length > 10 ? 2 : 4,
            pointHoverRadius: 6,
            borderWidth: 2
          },
          {
            label: 'Giá Mua',
            data: buyData,
            borderColor: '#22c55e',
            backgroundColor: 'rgba(34, 197, 94, 0.05)',
            fill: false,
            tension: 0.3,
            pointRadius: data.length > 10 ? 2 : 4,
            pointHoverRadius: 6,
            borderWidth: 2,
            borderDash: [4, 3]
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: {
            labels: {
              color: '#94a3b8',
              usePointStyle: true,
              pointStyle: 'line'
            }
          },
          tooltip: {
            backgroundColor: '#1e293b',
            borderColor: '#334155',
            borderWidth: 1,
            titleColor: '#f1f5f9',
            bodyColor: '#94a3b8',
            callbacks: {
              label: (ctx) =>
                ` ${ctx.dataset.label}: ${new Intl.NumberFormat('vi-VN').format(ctx.parsed.y ?? 0)} VND`
            }
          }
        },
        scales: {
          x: {
            ticks: {
              color: '#64748b',
              maxTicksLimit: 8,
              maxRotation: 30
            },
            grid: { color: '#1e293b' }
          },
          y: {
            ticks: {
              color: '#64748b',
              callback: (value) =>
                new Intl.NumberFormat('vi-VN', { notation: 'compact' }).format(Number(value))
            },
            grid: { color: '#1e293b' }
          }
        }
      }
    });
  }
}
