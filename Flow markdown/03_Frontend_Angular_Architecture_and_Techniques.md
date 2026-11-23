# Angular Frontend — Kiến Trúc & Kỹ Thuật Triển Khai (Frontend Architecture & Techniques)

> **Tài liệu tổng hợp kỹ thuật Frontend Angular** trong dự án GoldWatch. Tập trung chi tiết vào: Standalone Components, Reactive State với Signals, Tích hợp Chart.js, Service Layer, Error Interceptor, và Cấu hình Proxy chống lỗi CORS.

---

## 1. Cấu Trúc Tổng Thể & Standalone Components

Frontend GoldWatch được xây dựng trên nền tảng **Angular hiện đại (v17+)** sử dụng hoàn toàn **Standalone Components** (loại bỏ hoàn toàn kiến trúc `NgModule` truyền thống, giúp code gọn gàng, khởi động nhanh và dễ tối ưu Tree-shaking).

```text
goldwatch-angular/src/app/
│
├── core/                                     ◄── Tầng cốt lõi của ứng dụng
│   ├── models/
│   │   └── gold-price.model.ts               ◄── TypeScript Interfaces & Helpers
│   ├── services/
│   │   └── gold.service.ts                   ◄── Giao tiếp HTTP với Backend API
│   └── interceptors/
│       └── error.interceptor.ts              ◄── Bắt và chuẩn hóa lỗi HTTP tập trung
│
├── features/                                 ◄── Tầng chức năng (Feature Components)
│   ├── dashboard/                            ◄── Màn hình chính điều phối
│   │   ├── dashboard.component.ts
│   │   ├── dashboard.component.html
│   │   └── dashboard.component.scss
│   ├── price-chart/                          ◄── Biểu đồ giá vàng (Chart.js)
│   │   ├── price-chart.component.ts
│   │   ├── price-chart.component.html
│   │   └── price-chart.component.scss
│   └── gold-list/                            ◄── Bảng dữ liệu + CRUD Modals
│       ├── gold-list.component.ts
│       ├── gold-list.component.html
│       └── gold-list.component.scss
│
├── environments/
│   └── environment.ts                        ◄── Cấu hình môi trường (apiUrl: '/api')
│
├── app.config.ts                             ◄── Cấu hình App Providers
├── app.routes.ts                             ◄── Định tuyến ứng dụng
└── app.ts                                    ◄── Root Component
```

---

## 2. Quản Lý Trạng Thái Phản Ứng (Reactive State Management với Signals)

Thay vì dùng `BehaviorSubject` truyền thống của RxJS cho các biến trạng thái giao diện cơ bản, dự án áp dụng **Angular Signals** (`signal`):

```typescript
// features/dashboard/dashboard.component.ts
export class DashboardComponent implements OnInit {
  private readonly goldService = inject(GoldService);

  // Khai báo các Signal lưu trạng thái
  prices = signal<GoldPrice[]>([]);
  latestPrice = signal<GoldPrice | null>(null);
  isSyncing = signal(false);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  syncMessage = signal<string | null>(null);

  // ...
}
```

### Ưu điểm vượt trội của Signals:
1. **Fine-grained Reactivity:** Angular biết chính xác phần HTML nào đang đọc `prices()` để chỉ re-render lại đúng phần đó, không cần quét toàn bộ cây component (khác biệt lớn so với Zone.js cũ).
2. **Cú pháp ngắn gọn:** Đọc giá trị bằng `prices()`, cập nhật bằng `this.prices.set(newData)` mà không cần dùng pipe `async` trong template.
3. **Getter tính toán biến động giá tự động:**
   ```typescript
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
   ```

---

## 3. Tự Động Đồng Bộ Dữ Liệu Khi Load Trang (Auto-Sync Lifecycle)

Để giải quyết vấn đề khi mở web lần đầu chưa có dữ liệu quá khứ để vẽ biểu đồ, component tự động kích hoạt luồng đồng bộ:

```typescript
ngOnInit(): void {
  this.initDashboard();
}

initDashboard(): void {
  this.isLoading.set(true);
  this.errorMessage.set(null);

  // 1. Tự động sync 7 ngày qua trước
  this.goldService.syncPriceHistory(7).subscribe({
    next: () => {
      // 2. Sau khi sync thành công, tải toàn bộ DB lên
      this.loadPrices();
    },
    error: (err) => {
      // 3. Nếu mất mạng hoặc API ngoài lỗi, fallback tải dữ liệu cũ trong DB
      console.warn('Tự động sync gặp lỗi, fallback tải DB:', err);
      this.loadPrices();
    }
  });
}
```

---

## 4. Tích Hợp Chart.js Vẽ Biểu Đồ Giá Thời Gian Thực

Component [`PriceChartComponent`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/goldwatch-angular/src/app/features/price-chart/price-chart.component.ts) minh họa cách nhúng thư viện đồ họa Canvas bên thứ ba vào Angular một cách an toàn và tối ưu:

### 4.1. Tree-shaking Module Chart.js
Chỉ đăng ký đúng các module cần dùng để giảm dung lượng file bundle JS:
```typescript
import {
  Chart, LineController, LineElement, PointElement,
  LinearScale, CategoryScale, Tooltip, Legend, Filler
} from 'chart.js';

Chart.register(LineController, LineElement, PointElement, LinearScale, CategoryScale, Tooltip, Legend, Filler);
```

### 4.2. Vòng Đời Kết Nối Canvas (`ViewChild` & Lifecycle Hooks)
- Sử dụng `@ViewChild('chartCanvas')` để truy cập trực tiếp vào thẻ `<canvas>` trong template.
- Khởi tạo chart tại `ngAfterViewInit()` khi DOM đã render hoàn tất:
  ```typescript
  ngAfterViewInit(): void {
    this.isViewReady = true;
    this.buildChart();
  }
  ```
- **Tối ưu cập nhật khi Input thay đổi (`ngOnChanges`):** Thay vì hủy chart cũ và tạo mới gây giật màn hình (flicker), ta cập nhật trực tiếp dữ liệu và gọi `chart.update('active')`:
  ```typescript
  if (this.chart) {
    this.chart.data.labels = labels;
    this.chart.data.datasets[0].data = sellData;
    this.chart.data.datasets[1].data = buyData;
    this.chart.update('active');
    return;
  }
  ```
- **Chống rò rỉ bộ nhớ (Memory Leak):** Luôn gọi `this.chart?.destroy()` trong `ngOnDestroy()`.

---

## 5. Tầng Service & Xử Lý Lỗi Tập Trung (Error Interceptor)

### 5.1. `GoldService`
Sử dụng `HttpClient` được inject qua hàm `inject(HttpClient)` hiện đại:
```typescript
@Injectable({ providedIn: 'root' })
export class GoldService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/gold`;

  getAll(): Observable<GoldPrice[]> { ... }
  getById(id: number): Observable<GoldPrice> { ... }
  create(request: CreateGoldPriceRequest): Observable<GoldPrice> { ... }
  update(id: number, request: CreateGoldPriceRequest): Observable<void> { ... }
  delete(id: number): Observable<void> { ... }
  syncLatestPrice(goldType?: string): Observable<GoldPrice> { ... }
  syncPriceHistory(days: number = 7, goldType?: string): Observable<GoldPrice[]> { ... }
}
```

### 5.2. Functional HttpInterceptor
Được khai báo bằng `HttpInterceptorFn` (chuẩn Angular mới nhất, không cần kế thừa interface `HttpInterceptor` cồng kềnh):

```typescript
// core/interceptors/error.interceptor.ts
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

      console.error(`[GoldWatch HTTP ${error.status}]`, message, error);
      return throwError(() => ({ status: error.status, message }));
    })
  );
};
```

---

## 6. Cấu Hình Dev Proxy — Giải Pháp Triệt Để Chống Lỗi CORS

### 6.1. Vấn đề thường gặp khi phát triển Fullstack:
- Frontend chạy tại `http://localhost:4200`.
- Backend chạy tại `http://localhost:5248`.
- Khác port dẫn đến vi phạm chính sách **Same-Origin Policy** của trình duyệt, gây ra lỗi CORS preflight hoặc bị chặn bởi chính sách bảo mật SSL.

### 6.2. Giải pháp: Angular Reverse Proxy
Tạo file [`proxy.conf.json`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/goldwatch-angular/proxy.conf.json):
```json
{
  "/api": {
    "target": "http://localhost:5248",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "info"
  }
}
```
Và trong `angular.json`:
```json
"serve": {
  "builder": "@angular/build:dev-server",
  "options": {
    "proxyConfig": "proxy.conf.json"
  }
}
```
- Khi Angular gửi request tới `/api/gold`, trình duyệt xem request đó là **cùng nguồn (`localhost:4200`)**.
- Angular dev server ngầm forward request đó qua TCP tới backend `localhost:5248`.
- **Kết quả:** Trình duyệt không kích hoạt kiểm tra CORS, giải quyết triệt để 100% lỗi CORS và lỗi SSL chứng chỉ tự ký trên môi trường Development.

---

## 7. Thiết Kế Giao Diện & Hệ Thống Màu (Dark Gold Theme)

Giao diện áp dụng thiết kế hiện đại trên nền tối (Dark Theme) với điểm nhấn màu vàng kim (Gold):
- **Sử dụng CSS Custom Properties (Variables):**
  ```scss
  :root {
    --color-bg:          #0f172a; // Slate 900
    --color-surface-1:   #1e293b; // Slate 800
    --color-surface-2:   #334155; // Slate 700
    --color-border:      #334155;
    --color-text:        #f1f5f9;
    --color-text-muted:  #94a3b8;
    --color-gold:        #f59e0b; // Amber 500
    --color-gold-light:  #fbbf24; // Amber 400
  }
  ```
- **Responsive Layout:**
  - `price-cards` sử dụng `grid-template-columns: repeat(auto-fit, minmax(200px, 1fr))` tự động co giãn thẻ giá theo kích thước màn hình từ điện thoại đến máy tính để bàn.
  - Form modal hỗ trợ hiển thị 2 cột trên máy tính và tự chuyển 1 cột trên màn hình điện thoại (`@media (max-width: 480px)`).

