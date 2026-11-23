# GoldWatch API Documentation
> Dành cho Angular Frontend — V7 Integration

---

## Mục lục

1. [Tổng quan Backend](#1-tổng-quan-backend)
2. [Kết nối & Cấu hình](#2-kết-nối--cấu-hình)
3. [Data Models (TypeScript Interfaces)](#3-data-models-typescript-interfaces)
4. [API Endpoints](#4-api-endpoints)
5. [Response Formats & HTTP Status Codes](#5-response-formats--http-status-codes)
6. [CORS Policy](#6-cors-policy)
7. [Error Handling](#7-error-handling)
8. [Angular Service Layer Gợi ý](#8-angular-service-layer-gợi-ý)
9. [Hướng dẫn chạy Backend](#9-hướng-dẫn-chạy-backend)

---

## 1. Tổng quan Backend

| Thông tin | Giá trị |
|-----------|---------|
| **Framework** | ASP.NET Core (.NET 10) |
| **Database** | PostgreSQL (via Entity Framework Core) |
| **Base URL (dev)** | `http://localhost:5000` hoặc `https://localhost:7000` |
| **API Docs (Swagger)** | `http://localhost:5000/openapi/v1.json` *(chỉ chạy ở Development)* |
| **External Data Source** | [vang.today API](https://www.vang.today/api/) |

### Kiến trúc tổng quan

```text
Angular (localhost:4200)
        │
        │ HTTP (JSON)
        ▼
ASP.NET Core API (localhost:5000)
        │
        ├── GoldController  ──► GoldPriceService
        │                              │
        │                    ┌─────────┴─────────┐
        │                    ▼                   ▼
        │             ApplicationDbContext   VangTodayProvider
        │                    │                   │
        │               PostgreSQL           vang.today API
        │
        └── (V7 future) NewsController, AnalysisController
```

---

## 2. Kết nối & Cấu hình

### Base URL

```typescript
// environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

### CORS đã được cấu hình sẵn

Backend đã có CORS policy tên `AllowAngularDev` cho phép Angular dev server:

```
Origin: http://localhost:4200
Methods: GET, POST, PUT, DELETE (AllowAnyMethod)
Headers: AllowAnyHeader
```

> [!IMPORTANT]
> Chỉ cần Angular chạy đúng port `4200` là sẽ kết nối được. Không cần thêm cấu hình gì thêm phía frontend cho môi trường dev.

---

## 3. Data Models (TypeScript Interfaces)

Dựa trên `GoldPrice` model và DTOs của backend:

### `GoldPrice` — Entity trả về từ API

```typescript
// models/gold-price.model.ts
export interface GoldPrice {
  id: number;
  goldType: string;      // Ví dụ: "Vàng SJC 1L-10L", "SJL1L10"
  buyPrice: number;      // Đơn vị: VND (ví dụ: 14230000)
  sellPrice: number;     // Đơn vị: VND (ví dụ: 14450000)
  currency: string;      // Luôn là "VND" ở thời điểm hiện tại
  source: string;        // Nguồn dữ liệu: "vang.today" hoặc manual
  collectedAt: string;   // ISO 8601 datetime string (UTC)
}
```

### `CreateGoldPriceRequest` — Body khi tạo/cập nhật thủ công

```typescript
// models/create-gold-price-request.model.ts
export interface CreateGoldPriceRequest {
  goldType: string;   // Required, không được rỗng
  buyPrice: number;   // Required, > 0
  sellPrice: number;  // Required, > 0
  currency: string;   // Required
  source: string;     // Required
}
```

> [!NOTE]
> ASP.NET Core mặc định serialize JSON với **camelCase** (`goldType`, `buyPrice`, ...), nên Angular interface dùng camelCase là đúng.

---

## 4. API Endpoints

Base route: `/api/gold`

---

### 4.1 `GET /api/gold` — Lấy tất cả bản ghi giá vàng

| | |
|---|---|
| **Method** | `GET` |
| **URL** | `/api/gold` |
| **Auth** | Không yêu cầu (V7) |
| **Body** | Không có |

**Response: `200 OK`**
```json
[
  {
    "id": 1,
    "goldType": "Vàng SJC 1L-10L",
    "buyPrice": 14230000,
    "sellPrice": 14450000,
    "currency": "VND",
    "source": "vang.today",
    "collectedAt": "2026-11-06T04:30:00Z"
  },
  {
    "id": 2,
    "goldType": "Vàng SJC 1L-10L",
    "buyPrice": 14240000,
    "sellPrice": 14460000,
    "currency": "VND",
    "source": "vang.today",
    "collectedAt": "2026-11-06T05:00:00Z"
  }
]
```

**Mục đích frontend:** Dùng để hiển thị lịch sử giá, vẽ chart.

---

### 4.2 `GET /api/gold/{id}` — Lấy bản ghi theo ID

| | |
|---|---|
| **Method** | `GET` |
| **URL** | `/api/gold/{id}` |
| **Path Param** | `id` — integer |
| **Auth** | Không yêu cầu (V7) |

**Response: `200 OK`**
```json
{
  "id": 1,
  "goldType": "Vàng SJC 1L-10L",
  "buyPrice": 14230000,
  "sellPrice": 14450000,
  "currency": "VND",
  "source": "vang.today",
  "collectedAt": "2026-11-06T04:30:00Z"
}
```

**Response: `404 Not Found`** — Khi không tìm thấy ID.

---

### 4.3 `POST /api/gold` — Tạo bản ghi thủ công

| | |
|---|---|
| **Method** | `POST` |
| **URL** | `/api/gold` |
| **Content-Type** | `application/json` |
| **Auth** | Không yêu cầu (V7) |

**Request Body:**
```json
{
  "goldType": "Vàng SJC 1L-10L",
  "buyPrice": 14230000,
  "sellPrice": 14450000,
  "currency": "VND",
  "source": "manual"
}
```

**Validation Rules:**
| Field | Rule |
|-------|------|
| `goldType` | Required, không được rỗng |
| `buyPrice` | Required, phải > 0 |
| `sellPrice` | Required, phải > 0 |
| `currency` | Required |
| `source` | Required |

**Response: `201 Created`**
```json
{
  "id": 3,
  "goldType": "Vàng SJC 1L-10L",
  "buyPrice": 14230000,
  "sellPrice": 14450000,
  "currency": "VND",
  "source": "manual",
  "collectedAt": "2026-11-06T11:00:00Z"
}
```
> Location header: `/api/gold/3`

**Response: `400 Bad Request`** — Khi validation thất bại.

---

### 4.4 `PUT /api/gold/{id}` — Cập nhật bản ghi

| | |
|---|---|
| **Method** | `PUT` |
| **URL** | `/api/gold/{id}` |
| **Path Param** | `id` — integer |
| **Content-Type** | `application/json` |

**Request Body:** Giống `POST /api/gold`

**Response: `204 No Content`** — Cập nhật thành công (không có body).

**Response: `404 Not Found`** — Không tìm thấy ID.

> [!TIP]
> Angular: Sau khi nhận `204`, reload lại danh sách hoặc cập nhật local state thay vì chờ server trả dữ liệu mới.

---

### 4.5 `DELETE /api/gold/{id}` — Xóa bản ghi

| | |
|---|---|
| **Method** | `DELETE` |
| **URL** | `/api/gold/{id}` |
| **Path Param** | `id` — integer |

**Response: `204 No Content`** — Xóa thành công (không có body).

**Response: `404 Not Found`** — Không tìm thấy ID.

---

### 4.6 `POST /api/gold/sync` — Sync giá vàng mới nhất từ vang.today

| | |
|---|---|
| **Method** | `POST` |
| **URL** | `/api/gold/sync` |
| **Query Param** | `type` *(optional)* — Mã loại vàng |
| **Auth** | Không yêu cầu (V7) |
| **Body** | Không có |

**Query Parameter `type`:**
```
POST /api/gold/sync              → Dùng default type từ cấu hình ("SJL1L10")
POST /api/gold/sync?type=SJL1L10 → Lấy giá Vàng SJC 1L-10L
POST /api/gold/sync?type=DOJIHN  → Lấy loại vàng DOJI Hà Nội (nếu API hỗ trợ)
```

**Response: `200 OK`** — Trả về bản ghi vừa được sync và lưu vào DB:
```json
{
  "id": 10,
  "goldType": "Vàng SJC 1L-10L",
  "buyPrice": 14240000,
  "sellPrice": 14460000,
  "currency": "VND",
  "source": "vang.today",
  "collectedAt": "2026-11-06T11:30:00Z"
}
```

**Response: `500 Internal Server Error`** — Khi vang.today không phản hồi hoặc trả dữ liệu lỗi.

> [!NOTE]
> Endpoint này gọi ra ngoài internet (vang.today). Cần đảm bảo backend có kết nối mạng khi gọi.

---

## 5. Response Formats & HTTP Status Codes

### Tóm tắt Status Codes

| Status | Ý nghĩa | Endpoints |
|--------|---------|-----------|
| `200 OK` | Thành công, có body trả về | `GET all`, `GET by id`, `POST sync` |
| `201 Created` | Tạo mới thành công, có body | `POST` |
| `204 No Content` | Thành công, không có body | `PUT`, `DELETE` |
| `400 Bad Request` | Dữ liệu gửi lên không hợp lệ (validation) | `POST`, `PUT` |
| `404 Not Found` | Không tìm thấy resource | `GET by id`, `PUT`, `DELETE` |
| `500 Internal Server Error` | Lỗi phía server (vd: external API down) | `POST sync` |

### Format lỗi `400 Bad Request`

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "GoldType": ["GoldType cannot be empty"],
    "BuyPrice": ["Price must larger than 0"]
  }
}
```

---

## 6. CORS Policy

Backend đã cấu hình sẵn trong [`Program.cs`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/GoldWatch.Api/Program.cs):

```csharp
// Đã được đăng ký:
policy.WithOrigins("http://localhost:4200")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

| Setting | Giá trị |
|---------|---------|
| **Allowed Origin** | `http://localhost:4200` |
| **Allowed Methods** | Tất cả (`GET`, `POST`, `PUT`, `DELETE`, ...) |
| **Allowed Headers** | Tất cả |
| **Credentials** | Chưa được bật (không gửi cookie/auth header) |

> [!WARNING]
> Nếu Angular dev server chạy port khác (ví dụ `4201`), cần sửa `WithOrigins(...)` trong `Program.cs` phía backend. Từ V9 (Authentication), CORS sẽ được siết chặt hơn.

---

## 7. Error Handling

### Phía Backend — Hiện tại (V4)

Hiện tại backend **chưa có global exception handler**. Lỗi không được bắt sẽ trả về response 500 mặc định của ASP.NET.

### Gợi ý Angular Error Interceptor

```typescript
// interceptors/error.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { inject } from '@angular/core';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'Đã xảy ra lỗi không xác định';

      switch (error.status) {
        case 400:
          message = 'Dữ liệu không hợp lệ: ' + JSON.stringify(error.error?.errors ?? error.error);
          break;
        case 404:
          message = 'Không tìm thấy dữ liệu';
          break;
        case 500:
          message = 'Lỗi server. Vui lòng thử lại sau.';
          break;
        case 0:
          message = 'Không thể kết nối đến server. Backend có đang chạy không?';
          break;
      }

      console.error(`[HTTP Error ${error.status}]`, message, error);
      return throwError(() => ({ status: error.status, message }));
    })
  );
};
```

---

## 8. Angular Service Layer Gợi ý

### Cấu trúc Angular project đề xuất cho V7

```text
goldwatch-angular/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── services/
│   │   │   │   └── gold.service.ts        ← Gọi API /api/gold
│   │   │   ├── interceptors/
│   │   │   │   └── error.interceptor.ts
│   │   │   └── models/
│   │   │       └── gold-price.model.ts
│   │   │
│   │   ├── features/
│   │   │   ├── dashboard/
│   │   │   │   └── dashboard.component.ts  ← Màn hình chính
│   │   │   ├── gold-price/
│   │   │   │   ├── gold-list.component.ts  ← Bảng lịch sử giá
│   │   │   │   └── gold-sync.component.ts  ← Nút Sync giá mới nhất
│   │   │   └── price-chart/
│   │   │       └── price-chart.component.ts ← Biểu đồ
│   │   │
│   │   └── environments/
│   │       └── environment.ts
│   └── ...
```

### `GoldService` — Angular Service mẫu

```typescript
// core/services/gold.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GoldPrice, CreateGoldPriceRequest } from '../models/gold-price.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class GoldService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/gold`;

  /** GET /api/gold — Lấy tất cả bản ghi */
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
   * Sync giá mới nhất từ vang.today, lưu vào DB, trả về bản ghi mới
   */
  syncLatestPrice(goldType?: string): Observable<GoldPrice> {
    let params = new HttpParams();
    if (goldType) {
      params = params.set('type', goldType);
    }
    return this.http.post<GoldPrice>(`${this.baseUrl}/sync`, null, { params });
  }
}
```

### `app.config.ts` — Cấu hình `provideHttpClient`

```typescript
// app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([errorInterceptor]))
  ]
};
```

---

## 9. Hướng dẫn chạy Backend

### Prerequisites

- .NET 10 SDK
- PostgreSQL đang chạy
- Connection string được cấu hình trong `appsettings.json` hoặc `appsettings.Development.json`

### appsettings.json cần có

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=goldwatch;Username=postgres;Password=your_password"
  },
  "GoldPriceApi": {
    "BaseUrl": "https://www.vang.today/api/",
    "DefaultType": "SJL1L10",
    "TimeoutSeconds": 10
  }
}
```

### Chạy Backend

```bash
# Di chuyển vào thư mục project
cd GoldWatch.Api

# Apply migrations (nếu chưa chạy lần nào)
dotnet ef database update

# Chạy server
dotnet run
```

Backend sẽ chạy tại:
- `http://localhost:5000`
- `https://localhost:7000`

### Swagger UI

Truy cập Swagger (chỉ ở Development mode):
```
http://localhost:5000/openapi/v1.json
```

Hoặc nếu dùng Scalar/Swagger UI, cần cài thêm package và cấu hình riêng.

---

## Phụ lục: Mapping V4 → V7

Theo roadmap trong `All version flow.md`, hiện tại project đang ở **V4 (News Crawler)**. Bạn muốn nhảy lên **V7 (AI Analysis Pipeline)** để bắt đầu làm frontend.

Dưới đây là những endpoint đang hoạt động và sẽ hoạt động:

| Endpoint | V4 (hiện tại) | V5 | V6 | V7 |
|----------|---------------|----|----|-----|
| `GET /api/gold` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/gold/{id}` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/gold` | ✅ | ✅ | ✅ | ✅ |
| `PUT /api/gold/{id}` | ✅ | ✅ | ✅ | ✅ |
| `DELETE /api/gold/{id}` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/gold/sync` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/news` | ❌ | ❌ | ✅ | ✅ |
| `GET /api/news/{id}` | ❌ | ❌ | ✅ | ✅ |
| `GET /api/news/sources` | ❌ | ❌ | ✅ | ✅ |
| `GET /api/analysis/latest` | ❌ | ❌ | ❌ | ✅ |
| `GET /api/analysis/{articleId}` | ❌ | ❌ | ❌ | ✅ |

> [!IMPORTANT]
> Vì nhảy thẳng từ V4 lên V7 để làm Angular, **bạn sẽ chỉ có các Gold Price endpoints hoạt động ngay**. Các news/analysis endpoints cần implement thêm backend ở V5-V7 trước khi Angular có thể gọi được.

### Gợi ý thứ tự làm Angular khi nhảy V4 → V7

```text
Bước 1: Angular setup + Gold Price display
  → Dùng GET /api/gold, POST /api/gold/sync
  → Dashboard đơn giản hiển thị giá hiện tại

Bước 2: Price Chart
  → Dùng GET /api/gold, sort theo collectedAt
  → Vẽ line chart với Chart.js hoặc ngx-charts

Bước 3: CRUD UI
  → Dùng POST /api/gold, PUT /api/gold/{id}, DELETE /api/gold/{id}
  → Bảng quản lý thủ công

Bước 4: Chờ backend V5-V7 implement news/analysis
  → Rồi thêm NewsListComponent, AnalysisComponent
```

