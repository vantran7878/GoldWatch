import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Interface mô tả hình dạng dữ liệu trả về từ API
// Phải khớp với GoldPrice model bên backend C#
export interface GoldPrice {
  id: number;
  goldType: string;
  buyPrice: number;
  sellPrice: number;
  currency: string;
  source: string;
  collectedAt: string; // ISO 8601 string, ví dụ: "2024-10-08T02:00:00Z"
}

@Injectable({
  providedIn: 'root' // Service này có sẵn ở toàn bộ app, không cần khai báo thêm
})
export class GoldApiService {

  // URL gốc của ASP.NET Core API
  private readonly apiUrl = 'http://localhost:5248/api';

  // Angular tự inject HttpClient vào đây thông qua DI
  constructor(private http: HttpClient) {}

  // Lấy toàn bộ danh sách giá vàng
  // Observable<GoldPrice[]> = "luồng dữ liệu" trả về mảng GoldPrice
  getAll(): Observable<GoldPrice[]> {
    return this.http.get<GoldPrice[]>(`${this.apiUrl}/gold`);
  }

  // Lấy một bản ghi theo ID
  getById(id: number): Observable<GoldPrice> {
    return this.http.get<GoldPrice>(`${this.apiUrl}/gold/${id}`);
  }
}
