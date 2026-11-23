// Ánh xạ 1-1 với GoldPrice model của backend
export interface GoldPrice {
  id: number;
  goldType: string;
  buyPrice: number;
  sellPrice: number;
  currency: string;
  source: string;
  collectedAt: string; // ISO 8601 UTC string
}

// Ánh xạ với CreateGoldPriceRequest DTO của backend
export interface CreateGoldPriceRequest {
  goldType: string;
  buyPrice: number;
  sellPrice: number;
  currency: string;
  source: string;
}

// Helper: spread = sellPrice - buyPrice
export function calcSpread(price: GoldPrice): number {
  return price.sellPrice - price.buyPrice;
}

