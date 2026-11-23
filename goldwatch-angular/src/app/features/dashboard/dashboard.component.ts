import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { GoldService } from '../../core/services/gold.service';
import { GoldPrice } from '../../core/models/gold-price.model';
import { PriceChartComponent } from '../price-chart/price-chart.component';
import { GoldListComponent } from '../gold-list/gold-list.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, PriceChartComponent, GoldListComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly goldService = inject(GoldService);

  prices = signal<GoldPrice[]>([]);
  latestPrice = signal<GoldPrice | null>(null);
  isSyncing = signal(false);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  syncMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadPrices();
  }

  loadPrices(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.goldService.getAll().subscribe({
      next: (data) => {
        // Sắp xếp theo thời gian mới nhất trước
        const sorted = [...data].sort(
          (a, b) => new Date(b.collectedAt).getTime() - new Date(a.collectedAt).getTime()
        );
        this.prices.set(sorted);
        this.latestPrice.set(sorted[0] ?? null);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.message ?? 'Không thể tải dữ liệu');
        this.isLoading.set(false);
      }
    });
  }

  syncPrice(): void {
    this.isSyncing.set(true);
    this.syncMessage.set(null);

    this.goldService.syncLatestPrice().subscribe({
      next: (newPrice) => {
        this.syncMessage.set(`✅ Đã sync: ${newPrice.goldType} — Mua: ${this.formatPrice(newPrice.buyPrice)}`);
        this.isSyncing.set(false);
        this.loadPrices(); // Reload toàn bộ danh sách
      },
      error: (err) => {
        this.syncMessage.set(`❌ ${err.message ?? 'Sync thất bại'}`);
        this.isSyncing.set(false);
      }
    });
  }

  formatPrice(value: number): string {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }).format(value);
  }

  get priceChange(): { value: number; percent: number; direction: 'up' | 'down' | 'flat' } {
    const list = this.prices();
    if (list.length < 2) return { value: 0, percent: 0, direction: 'flat' };

    const latest = list[0].sellPrice;
    const previous = list[1].sellPrice;
    const diff = latest - previous;
    const percent = previous > 0 ? (diff / previous) * 100 : 0;

    return {
      value: diff,
      percent,
      direction: diff > 0 ? 'up' : diff < 0 ? 'down' : 'flat'
    };
  }
}

