import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GoldPrice } from './gold-price';

describe('GoldPrice', () => {
  let component: GoldPrice;
  let fixture: ComponentFixture<GoldPrice>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GoldPrice],
    }).compileComponents();

    fixture = TestBed.createComponent(GoldPrice);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
