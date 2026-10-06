# Giải Thích Chuyên Sâu: Bước 5 & Bước 6 (DbContext, Service Lifetime & Migrations)

Tài liệu này giải thích chi tiết hai bước mang tính bước ngoặt trong **Version 2**:
* **Bước 5:** Đăng ký `ApplicationDbContext` trong `Program.cs` và bài toán **Service Lifetime (Captive Dependency)**.
* **Bước 6:** Cơ chế hoạt động của **Entity Framework Core Migrations**, bảng `__EFMigrationsHistory` và quá trình đồng bộ schema xuống database PostgreSQL.

---

## MỤC LỤC
1. [BƯỚC 5: ĐĂNG KÝ DBCONTEXT & NGUY CƠ CAPTIVE DEPENDENCY](#1-bước-5-đăng-ký-dbcontext--nguy-cơ-captive-dependency)
   * [1.1. `AddDbContext` thực chất làm những gì?](#11-adddbcontext-thực-chất-làm-những-gì)
   * [1.2. Tại sao `DbContext` bắt buộc phải là Scoped?](#12-tại-sao-dbcontext-bắt-buộc-phải-là-scoped)
   * [1.3. Thảm họa Captive Dependency: Khi Singleton "ôm" Scoped](#13-thảm-họa-captive-dependency-khi-singleton-ôm-scoped)
   * [1.4. Bảng tổng kết vòng đời dịch vụ trong Web API](#14-bảng-tổng-kết-vòng-đời-dịch-vụ-trong-web-api)
2. [BƯỚC 6: CƠ CHẾ HOẠT ĐỘNG CỦA EF CORE MIGRATIONS](#2-bước-6-cơ-chế-hoạt-động-của-ef-core-migrations)
   * [2.1. Triết lý Code-First là gì?](#21-triết-lý-code-first-là-gì)
   * [2.2. Mổ xẻ lệnh `dotnet ef migrations add InitialCreate`](#22-mổ-xẻ-lệnh-dotnet-ef-migrations-add-initialcreate)
   * [2.3. Bảng ánh xạ kiểu dữ liệu (C# -> PostgreSQL)](#23-bảng-ánh-xạ-kiểu-dữ-liệu-c---postgresql)
   * [2.4. Mổ xẻ lệnh `dotnet ef database update`](#24-mổ-xẻ-lệnh-dotnet-ef-database-update)
   * [2.5. Giải mã log: Tại sao xuất hiện `fail: SELECT FROM __EFMigrationsHistory`?](#25-giải-mã-log-tại-sao-xuất-hiện-fail-select-from-__efmigrationshistory)
3. [TÓM TẮT CHECKLIST PHỎNG VẤN](#3-tóm-tắt-checklist-phỏng-vấn)

---

## 1. BƯỚC 5: ĐĂNG KÝ DBCONTEXT & NGUY CƠ CAPTIVE DEPENDENCY

Trong `Program.cs`, bạn đã thêm:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Thay vì AddSingleton, ta đổi thành AddScoped:
builder.Services.AddScoped<IGoldPriceService, GoldPriceService>();
```

### 1.1. `AddDbContext` thực chất làm những gì?
Khi bạn gọi `builder.Services.AddDbContext<ApplicationDbContext>(...)`:
1. **Đăng ký DbContext với Lifetime mặc định là `Scoped`**: Mỗi khi có một HTTP Request gửi đến API, container sẽ tạo **1 instance `ApplicationDbContext` duy nhất** cho request đó. Khi request kết thúc (trả response về cho client), instance này được giải phóng (`Dispose`), đóng kết nối DB.
2. **Cấu hình Npgsql Provider**: `options.UseNpgsql(connectionString)` thiết lập driver PostgreSQL để EF Core biết phải sinh ra câu lệnh SQL đặc thù của Postgres (ví dụ kiểu `numeric`, cú pháp phân trang `LIMIT/OFFSET`, identity column).

---

### 1.2. Tại sao `DbContext` bắt buộc phải là Scoped mà không được là Singleton?

`DbContext` trong Entity Framework Core có 3 đặc điểm nội tại:
1. **Không an toàn luồng (Not Thread-Safe):** Một instance `DbContext` không được phép thực thi đồng thời nhiều câu lệnh truy vấn trên các luồng khác nhau.
2. **Bộ nhớ theo dõi thay đổi (Change Tracker):** Mỗi khi bạn query hay thêm đối tượng, `DbContext` sẽ lưu trữ tham chiếu đối tượng đó trong RAM để theo dõi trạng thái (`Added`, `Modified`, `Unchanged`). Nếu là Singleton, sau vài ngày chạy, bộ nhớ RAM sẽ phình to khủng khiếp (Memory Leak) và tốc độ query sẽ ngày càng chậm.
3. **Mỗi request là một Unit of Work độc lập:** Request của người dùng A cập nhật giá vàng không được phép dính líu hay dùng chung transaction/connection với request của người dùng B.

---

### 1.3. Thảm họa Captive Dependency: Khi Singleton "ôm" Scoped

Đây là một trong những câu hỏi phỏng vấn kinh điển nhất về Dependency Injection trong .NET.

#### Hiện tượng xảy ra nếu giữ `GoldPriceService` là Singleton:

```text
       [DI Container]
             │
             ▼
  ┌──────────────────────┐  (Sống mãi mãi trong suốt vòng đời App)
  │   GoldPriceService   │  <-- Đăng ký là SINGLETON
  │     (Singleton)      │
  └──────────┬───────────┘
             │  (Giữ chặt tham chiếu)
             ▼
  ┌──────────────────────┐  (Được tạo cho Request đầu tiên nhưng KHÔNG BAO GIỜ bị hủy)
  │ ApplicationDbContext │  <-- Đăng ký là SCOPED
  │       (Scoped)       │
  └──────────────────────┘
```

* Khi server khởi động và nhận Request số 1, `GoldPriceService` được tạo ra. Nó yêu cầu inject `ApplicationDbContext`.
* Container cấp cho nó một `DbContext` (thuộc Request số 1).
* Vì `GoldPriceService` là **Singleton** (không bao giờ chết), nó sẽ **bắt giữ vĩnh viễn (captive)** instance `DbContext` này trong biến private field của nó.
* Đến Request số 2, Request số 3... đến Request số 1000: Tất cả đều dùng chung đúng một `DbContext` của Request số 1!
* **Hậu quả:** 
  * Exception nổ ra ngay lập tức khi 2 request đến cùng lúc: `A second operation was started on this context instance before a previous operation completed`.
  * Dữ liệu cũ bị cache trong Change Tracker, dẫn đến kết quả sai lệch.
  * Trong môi trường Development, ASP.NET Core có chế độ kiểm tra nghiêm ngặt (`ValidateScopes = true`), nó sẽ chặn đứng và làm crash ứng dụng ngay khi khởi động:
    > `System.InvalidOperationException: Cannot consume scoped service 'ApplicationDbContext' from singleton 'IGoldPriceService'.`

#### Cách giải quyết chuẩn:
Quy tắc vàng của DI: **Một Service không bao giờ được có vòng đời dài hơn (longer-lived) dịch vụ mà nó phụ thuộc vào.**
$$\text{Singleton} \longrightarrow \text{KHÔNG ĐƯỢC PHỤ THUỘC VÀO} \longrightarrow \text{Scoped/Transient}$$
Vì vậy, khi `GoldPriceService` cần dùng `ApplicationDbContext` (Scoped), bản thân `GoldPriceService` **bắt buộc phải là Scoped**.

---

### 1.4. Bảng tổng kết vòng đời dịch vụ trong Web API

| Vòng đời | Thời điểm tạo | Thời điểm hủy | Trường hợp sử dụng điển hình |
| :--- | :--- | :--- | :--- |
| **`Transient`** | Mỗi lần được gọi/inject ở bất kỳ đâu | Khi đối tượng chứa nó bị hủy | Các dịch vụ tính toán nhẹ, format chuỗi, không lưu trạng thái. |
| **`Scoped`** | 1 lần duy nhất cho mỗi **HTTP Request** | Khi HTTP Request kết thúc | **`DbContext`**, Business Service (`GoldPriceService`), Repository. |
| **`Singleton`** | 1 lần duy nhất lúc app khởi động | Khi toàn bộ Web Server tắt | In-memory cache, Background queue, cấu hình hệ thống. |

---

## 2. BƯỚC 6: CƠ CHẾ HOẠT ĐỘNG CỦA EF CORE MIGRATIONS

### 2.1. Triết lý Code-First là gì?
Trong quá khứ (Database-First), lập trình viên phải mở pgAdmin hoặc SQL Server Management Studio lên, gõ lệnh `CREATE TABLE ...`, sau đó vào code C# tạo class khớp với bảng đó.

EF Core áp dụng triết lý **Code-First**:
* Bạn chỉ cần tập trung thiết kế các class C# (như [`GoldPrice`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/GoldWatch.Api/Models/GoldPrice.cs)) và khai báo `DbSet<GoldPrice>` trong `DbContext`.
* EF Core sẽ **tự động sinh ra các câu lệnh SQL DDL** (`CREATE TABLE`, `ALTER TABLE`, `ADD CONSTRAINT`) thông qua công cụ **Migrations**.

---

### 2.2. Mổ xẻ lệnh `dotnet ef migrations add InitialCreate`

Khi bạn gõ lệnh này, EF Core thực hiện các bước sau trong hậu trường:
1. **Biên dịch code (Build):** Đảm bảo dự án không có lỗi cú pháp.
2. **So sánh Model với Snapshot:**
   * Nó đọc file `ApplicationDbContextModelSnapshot.cs` (nếu chưa có thì coi như rỗng).
   * Nó soi class `GoldPrice` xem có các property nào (`Id`, `GoldType`, `BuyPrice`, ...).
   * Nó nhận diện sự khác biệt: *"Có 1 entity mới tên GoldPrice chưa có trong database"*.
3. **Sinh ra thư mục `Migrations/` gồm 2 file:**
   * `[Timestamp]_InitialCreate.cs`: Chứa code C# mô tả sự thay đổi:
     * **`Up(MigrationBuilder migrationBuilder)`**: Code để tạo bảng `GoldPrices`.
     * **`Down(MigrationBuilder migrationBuilder)`**: Code để xóa bảng `GoldPrices` (nếu cần hoàn tác/rollback).
   * `ApplicationDbContextModelSnapshot.cs`: Bức ảnh chụp toàn bộ trạng thái hiện tại của Database. Sau này khi bạn thêm cột mới, EF Core sẽ so sánh class C# với snapshot này để biết bạn vừa sửa cái gì.

---

### 2.3. Bảng ánh xạ kiểu dữ liệu (C# -> PostgreSQL)

Nhìn vào file migration bạn vừa mở:

```csharp
migrationBuilder.CreateTable(
    name: "GoldPrices",
    columns: table => new
    {
        Id = table.Column<int>(type: "integer", nullable: false)
            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
        GoldType = table.Column<string>(type: "text", nullable: false),
        BuyPrice = table.Column<decimal>(type: "numeric", nullable: false),
        SellPrice = table.Column<decimal>(type: "numeric", nullable: false),
        Currency = table.Column<string>(type: "text", nullable: false),
        Source = table.Column<string>(type: "text", nullable: false),
        CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_GoldPrices", x => x.Id);
    });
```

EF Core và driver Npgsql đã tự động dịch các kiểu dữ liệu C# sang chuẩn PostgreSQL:

| C# Property | PostgreSQL Data Type | Giải thích kỹ thuật |
| :--- | :--- | :--- |
| `int Id` | `integer` + `IdentityByDefaultColumn` | Cột số nguyên tự tăng (auto-increment primary key). |
| `string GoldType` | `text` | Chuỗi ký tự độ dài tùy ý trong Postgres. |
| `decimal BuyPrice` | `numeric` | Kiểu số chính xác tuyệt đối, tránh sai số dấu phẩy động cho tiền tệ. |
| `DateTime CollectedAt` | `timestamp with time zone` | Kiểu ngày giờ kèm múi giờ (UTC), chuẩn quốc tế trong Postgres. |

---

### 2.4. Mổ xẻ lệnh `dotnet ef database update`

Lệnh này làm nhiệm vụ: **Biến file C# migration thành SQL thật và thực thi trực tiếp trên PostgreSQL.**

Quy trình diễn ra như sau:

```text
[dotnet ef database update]
            │
            ▼
1. Kết nối đến PostgreSQL qua ConnectionString
            │
            ▼
2. Kiểm tra xem bảng "__EFMigrationsHistory" đã có chưa?
      ├── Chưa có -> Tự động chạy CREATE TABLE "__EFMigrationsHistory"
      └── Đã có   -> SELECT xem migration nào ĐÃ CHẠY rồi
            │
            ▼
3. So sánh danh sách file Migration trong code với bảng lịch sử:
   - File '20261006033758_InitialCreate' chưa có trong DB!
            │
            ▼
4. Bắt đầu một Database Transaction:
   - Thực thi lệnh: CREATE TABLE "GoldPrices" (...)
   - Ghi một dòng vào bảng "__EFMigrationsHistory":
     INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
     VALUES ('20261006033758_InitialCreate', '10.0.12');
            │
            ▼
5. Commit Transaction -> Hoàn tất!
```

---

### 2.5. Giải mã log: Tại sao xuất hiện `fail: SELECT FROM __EFMigrationsHistory`?

Trong terminal của bạn xuất hiện đoạn log:
```text
fail: Microsoft.EntityFrameworkCore.Database.Command[20102]
      Failed executing DbCommand (18ms)
      SELECT "MigrationId", "ProductVersion"
      FROM "__EFMigrationsHistory"
      ORDER BY "MigrationId";
...
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20261006033758_InitialCreate'.
Done.
```

#### Tại sao lại "fail"?
* Khi bạn chạy `dotnet ef database update` trên một database **mới tinh**:
  1. EF Core gửi lệnh `SELECT ... FROM "__EFMigrationsHistory"` để xem database đã chạy đến migration nào rồi.
  2. Tuy nhiên, vì database còn trống trơn, PostgreSQL báo lỗi: *"Bảng __EFMigrationsHistory không tồn tại!"*
  3. Lớp logging của EF Core ghi nhận câu lệnh SQL đó bị fail (đây là log cấp độ Command).
* **Nhưng EF Core đã bắt được lỗi này theo đúng thiết kế (expected exception):**
  * Ngay khi thấy bảng lịch sử chưa có, EF Core hiểu ngay: *"À, đây là database mới, mình cần tạo bảng lịch sử trước"*.
  * Nó lập tức tạo bảng `__EFMigrationsHistory`, sau đó áp dụng migration `InitialCreate`.
* **Kết luận:** Chữ `fail` đó chỉ là log thông báo của câu SELECT thăm dò ban đầu. Kết quả cuối cùng là `Applying migration ... Done` với exit code `0` nghĩa là **toàn bộ quá trình thành công tuyệt đối**.

---

## 3. TÓM TẮT CHECKLIST PHỎNG VẤN

1. **Captive Dependency là gì?**
   * Là hiện tượng một service có vòng đời dài (ví dụ `Singleton`) giữ tham chiếu đến một service có vòng đời ngắn hơn (ví dụ `Scoped`), khiến service ngắn hạn đó không thể giải phóng theo đúng vòng đời của nó.
2. **Tại sao `DbContext` không nên đăng ký Singleton?**
   * Vì không thread-safe, bộ nhớ Change Tracker bị phình to (Memory leak), và vi phạm nguyên tắc cách ly giao dịch (Unit of Work) giữa các HTTP Request.
3. **Bảng `__EFMigrationsHistory` dùng để làm gì?**
   * Để lưu danh sách các mã migration đã được áp dụng vào database, giúp EF Core biết chỉ chạy những migration mới chưa từng được thực thi.
4. **Cặp phương thức `Up()` và `Down()` trong migration có vai trò gì?**
   * `Up()` thực thi các thay đổi mới nhất (nâng cấp schema).
   * `Down()` hoàn tác các thay đổi đó (rollback về trạng thái trước khi chạy migration).
