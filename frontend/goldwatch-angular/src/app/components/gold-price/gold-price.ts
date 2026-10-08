import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GoldApiService, GoldPrice } from '../../services/gold-api';

@Component({
  imports: [CommonModule],
  selector: 'app-gold-price',
  styleUrl: './gold-price.css',
  templateUrl: './gold-price.html',
})
export class GoldPriceComponent implements OnInit {
  private goldApiService = inject(GoldApiService);

  prices = signal<GoldPrice[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  ngOnInit(): void {
    this.goldApiService.getAll().subscribe({
      next: (data) => {
        this.prices.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set('Cannot load data. Does API is running?');
        this.isLoading.set(false);
        console.error(err);
      }
    })
  }
}
