import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GoldPrice, CreateGoldPriceRequest } from '../models/gold-price.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class GoldService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/gold`;

  /** GET /api/gold — Toàn bộ lịch sử giá */
  getAll(): Observable<GoldPrice[]> {
    return this.http.get<GoldPrice[]>(this.baseUrl);
  }

  /** GET /api/gold/:id */
  getById(id: number): Observable<GoldPrice> {
    return this.http.get<GoldPrice>(`${this.baseUrl}/${id}`);
  }

  /** POST /api/gold — Tạo thủ công */
  create(request: CreateGoldPriceRequest): Observable<GoldPrice> {
    return this.http.post<GoldPrice>(this.baseUrl, request);
  }

  /** PUT /api/gold/:id */
  update(id: number, request: CreateGoldPriceRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  /** DELETE /api/gold/:id */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * POST /api/gold/sync?type={goldType}
   * Gọi vang.today, lưu DB, trả về bản ghi mới nhất
   */
  syncLatestPrice(goldType?: string): Observable<GoldPrice> {
    let params = new HttpParams();
    if (goldType) {
      params = params.set('type', goldType);
    }
    return this.http.post<GoldPrice>(`${this.baseUrl}/sync`, null, { params });
  }
}

