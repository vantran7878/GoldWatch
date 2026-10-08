import { Component, signal } from '@angular/core';
import { GoldPriceComponent } from './components/gold-price/gold-price';

@Component({
  imports: [GoldPriceComponent],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('goldwatch-angular');
}
