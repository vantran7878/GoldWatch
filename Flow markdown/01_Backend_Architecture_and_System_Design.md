# GoldWatch — Kiến Trúc Hệ Thống & Thiết Kế Phần Mềm (System Architecture & Software Design)

> **Tài liệu tổng hợp kiến trúc phần mềm** của dự án GoldWatch (Fullstack Web Application theo dõi, phân tích và lưu trữ biến động giá vàng).

---

## 1. Tổng Quan Hệ Thống (System Overview)

GoldWatch là hệ thống theo dõi giá vàng thời gian thực và lịch sử, được thiết kế theo mô hình **Client - Server (Decoupled Architecture)** kết hợp lưu trữ cơ sở dữ liệu quan hệ và tích hợp dữ liệu từ dịch vụ bên thứ ba (External API).

```text
                               ┌────────────────────────────────────────────────────────┐
                               │                    NGƯỜI DÙNG / BROWSER                │
                               └───────────────────────────┬────────────────────────────┘
                                                           │ HTTP (Port 4200)
                                                           ▼
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                                 FRONTEND (ANGULAR)                                                    │
│                                                                                                                       │
│   DashboardComponent ────► PriceChartComponent (Chart.js) ────► GoldListComponent (CRUD Modal)                        │
│            │                                                                                                          │
│            ▼ (Signals & Services)                                                                                     │
│       GoldService ────────► ErrorInterceptor ────────► Reverse Proxy (proxy.conf.json)                               │
└──────────────────────────────────────────────────────────┬────────────────────────────────────────────────────────────┘
                                                           │ HTTP Forward /api/* (Port 5248)
                                                           ▼
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                            BACKEND (ASP.NET CORE WEB API)                                             │
│                                                                                                                       │
│   [Controllers]                                                                                                       │
│        └── GoldController  (Routing, HTTP Action Results, Model Validation)                                           │
│                 │                                                                                                     │
│                 ▼                                                                                                     │
│   [Application Services]                                                                                              │
│        └── IGoldPriceService ──► GoldPriceService (Business logic, Deduplication, Idempotency)                        │
│                 │                                      │                                                              │
│                 ▼                                      ▼                                                              │
│   [External Integration]                     [Data Persistence]                                                       │
│   IGoldPriceProvider                         ApplicationDbContext (EF Core)                                           │
│        └── VangTodayGoldPriceProvider                  │                                                              │
│                 │ (Typed HttpClient)                   │ (Npgsql Provider)                                            │
└─────────────────┼──────────────────────────────────────┼──────────────────────────────────────────────────────────────┘
                  │                                      │
                  ▼                                      ▼
     ┌────────────────────────┐             ┌─────────────────────────┐
     │  External API Provider │             │   PostgreSQL Database   │
     │      (vang.today)      │             │  (Docker on Port 5432)  │
     └────────────────────────┘             └─────────────────────────┘
```

---

## 2. Kiến Trúc Phân Lớp (Layered Architecture)

Backend được tổ chức theo kiến trúc phân lớp hướng trách nhiệm (Separation of Concerns - SoC):

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ 1. API / Presentation Layer                                              │
│    Controllers, Action Filters, Routing, Response Format                 │
├──────────────────────────────────────────────────────────────────────────┤
│ 2. Application & Business Logic Layer                                    │
│    Services, Business Rules, Data Validation, Deduplication              │
├──────────────────────────────────────────────────────────────────────────┤
│ 3. External Integration Layer                                            │
│    Providers, Typed HttpClients, External DTOs, Third-party Mapping      │
├──────────────────────────────────────────────────────────────────────────┤
│ 4. Data Access & Persistence Layer                                       │
│    EF Core DbContext, Entities, Migrations, Npgsql Driver                │
└──────────────────────────────────────────────────────────────────────────┘
```

### 2.1. Tầng Presentation (API Layer)
- **Class chính:** `GoldController`
- **Nhiệm vụ:**
  - Nhận HTTP requests từ Client (`GET`, `POST`, `PUT`, `DELETE`).
  - Ánh xạ route token: `[Route("api/[controller]")]` -> `/api/gold`.
  - Tự động kiểm tra Model Validation nhờ attribute `[ApiController]`.
  - Trả về chuẩn HTTP Status Code (`200 OK`, `201 Created`, `204 NoContent`, `400 BadRequest`, `404 NotFound`).
  - **Không chứa logic truy vấn cơ sở dữ liệu**; ủy quyền toàn bộ cho tầng Service thông qua Dependency Injection.

### 2.2. Tầng Business Logic (Service Layer)
- **Interface:** `IGoldPriceService`
- **Implementation:** `GoldPriceService`
- **Nhiệm vụ:**
  - Điều phối luồng xử lý: vừa làm việc với cơ sở dữ liệu (`ApplicationDbContext`), vừa làm việc với dịch vụ bên ngoài (`IGoldPriceProvider`).
  - Đảm bảo tính **Idempotent (tính bất biến khi gọi nhiều lần)**: Kiểm tra dữ liệu trùng lặp trước khi thêm vào cơ sở dữ liệu để tránh vi phạm Unique Constraint.
  - Xử lý nghiệp vụ đọc/ghi bất đồng bộ (`async`/`await`).

### 2.3. Tầng External Integration (Provider Layer)
- **Interface:** `IGoldPriceProvider`
- **Implementation:** `VangTodayGoldPriceProvider`
- **Nhiệm vụ:**
  - Tách biệt hoàn toàn API bên thứ ba ra khỏi logic cốt lõi của ứng dụng.
  - Sử dụng **Typed HttpClient** được đăng ký trong ASP.NET Core DI Container.
  - Phụ trách việc serialize/deserialize dữ liệu JSON của bên thứ ba sang `External DTOs`, sau đó map sang `Domain Model` (`GoldPrice`).

### 2.4. Tầng Data Access (Persistence Layer)
- **Class chính:** `ApplicationDbContext` (kế thừa từ `DbContext` của EF Core).
- **Nhiệm vụ:**
  - Quản lý ánh xạ Object-Relational Mapping (ORM) giữa C# Model và các bảng trong PostgreSQL.
  - Quản lý vòng đời `ChangeTracker`, sinh mã SQL tự động và thực thi Transaction an toàn.

---

## 3. Các Design Patterns & Nguyên Lý Thiết Kế Đã Áp Dụng

### 3.1. Dependency Injection (DI) & Inversion of Control (IoC)
- Thay vì tự khởi tạo (`new`) các phụ thuộc, các class yêu cầu interface thông qua Constructor:
  ```csharp
  public GoldController(IGoldPriceService goldPriceService) { ... }
  public GoldPriceService(ApplicationDbContext context, IGoldPriceProvider goldPriceProvider) { ... }
  ```
- **Vòng đời dịch vụ (Service Lifetimes):**
  - `ApplicationDbContext`: **Scoped** (1 phiên kết nối trên mỗi HTTP request).
  - `IGoldPriceService`: **Scoped**.
  - `IGoldPriceProvider`: Đăng ký qua Typed Client (`AddHttpClient<IGoldPriceProvider, VangTodayGoldPriceProvider>()`), tái sử dụng `HttpMessageHandler` giúp chống cạn kiệt socket (Socket Exhaustion).

### 3.2. Provider / Strategy Pattern
- `IGoldPriceProvider` đóng vai trò là một Strategy abstraction.
- Nếu sau này hệ thống muốn đổi từ `vang.today` sang nguồn khác (như `SjcGoldPriceProvider` hay `BaoTinMinhChauProvider`), chỉ cần viết thêm một class triển khai `IGoldPriceProvider` và thay đổi đăng ký trong `Program.cs` mà **không cần sửa một dòng code nào trong `GoldPriceService`**.
- Tuân thủ nguyên lý **Open/Closed Principle (OCP)** của SOLID.

### 3.3. DTO Pattern (Data Transfer Object)
- Tách biệt 3 loại Model:
  1. **Domain Entity (`GoldPrice`):** Đại diện cho bảng trong Database.
  2. **Client DTO (`CreateGoldPriceRequest`):** Ràng buộc dữ liệu người dùng gửi lên qua Form với Data Annotations.
  3. **External DTO (`VangTodayPriceResponse`, `VangTodayHistoryResponse`):** Khớp với cấu trúc JSON của bên thứ ba.
- Giúp bảo vệ cấu trúc DB nội bộ, tránh over-posting và tránh phụ thuộc vào schema của bên thứ ba.

---

## 4. Các Luồng Xử Lý Dữ Liệu (Data Flow)

### 4.1. Luồng Sync Giá Hiện Tại (`POST /api/gold/sync`)
```text
Client (Web)
    │  1. POST /api/gold/sync?type=SJL1L10
    ▼
GoldController.SyncLatestPrice()
    │  2. SyncLatestPriceAsync()
    ▼
GoldPriceService
    │  3. FetchLatestPriceAsync()
    ▼
VangTodayGoldPriceProvider
    │  4. HTTP GET https://www.vang.today/api/prices?type=SJL1L10
    ▼
External API (vang.today)
    │  5. JSON Response { success: true, buy: ..., sell: ..., timestamp: ... }
    ▼
VangTodayGoldPriceProvider
    │  6. Map JSON DTO -> GoldPrice Model
    ▼
GoldPriceService
    │  7. Query DB: Kiểm tra (GoldType, Source, CollectedAt) đã tồn tại chưa?
    │       ├── Nếu ĐÃ TỒN TẠI ──► Trả về bản ghi cũ (không thêm trùng)
    │       └── Nếu CHƯA CÓ   ──► Add() vào DbSet -> SaveChangesAsync()
    ▼
GoldController ──► 8. Return 200 OK (GoldPrice JSON) ──► Client
```

### 4.2. Luồng Tự Động Sync 7 Ngày Quá Khứ (`POST /api/gold/sync-history`)
```text
Web App Load (ngOnInit)
    │  1. POST /api/gold/sync-history?days=7
    ▼
GoldPriceService.SyncPriceHistoryAsync()
    │  2. Gọi VangTodayGoldPriceProvider.FetchPriceHistoryAsync()
    ▼
VangToday Provider
    │  3. HTTP GET prices?type={code}&days=7
    │  4. Duyệt response.History -> map thành List<GoldPrice> (UTC datetime)
    ▼
GoldPriceService
    │  5. Deduplication check:
    │     - Lấy danh sách CollectedAt từ kết quả trả về
    │     - Query các ngày đã có sẵn trong DB
    │     - Lọc bỏ các ngày đã tồn tại: newPrices = history.Where(!existing)
    │  6. Nếu newPrices.Count > 0: AddRange(newPrices) -> SaveChangesAsync()
    │  7. Trả về toàn bộ danh sách 7 ngày
    ▼
Client ──► Nhận data ──► Tự động vẽ Line Chart và cập nhật Bảng Lịch Sử
```

---

## 5. Giải Quyết Vấn Đề Hạ Tầng & Kết Nối (Infrastructure & Networking)

| Vấn đề thực tế | Nguy cơ | Giải pháp kiến trúc đã triển khai |
|----------------|---------|-----------------------------------|
| **CORS Policy** | Trình duyệt chặn request từ `http://localhost:4200` sang `http://localhost:5248`. | Cấu hình CORS mở rộng cho mọi origin `localhost` và `127.0.0.1` trong `Program.cs`. |
| **Angular Dev Proxy** | Trình duyệt vẫn có thể lỗi nếu port thay đổi hoặc gặp SSL. | Cấu hình `proxy.conf.json` trong Angular để dev-server forward `/api/*` trực tiếp ngầm định. |
| **HTTPS Redirection** | Dev chạy HTTP nhưng bị 307 redirect sang HTTPS gây vỡ CORS/SSL. | Chỉ kích hoạt `UseHttpsRedirection()` khi `!app.Environment.IsDevelopment()`. |
| **Database Concurrency** | Nhiều lần sync cùng 1 mốc thời gian vi phạm Unique Key `23505`. | Xử lý Idempotency tại tầng Service: kiểm tra tồn tại trước khi `Add/AddRange`. |
| **Database Containerization** | Khó khăn khi setup môi trường cơ sở dữ liệu cục bộ. | Sử dụng Docker container `goldwatch-postgres` chạy độc lập trên port 5432. |

