# C#, .NET & Entity Framework Core — Kiến Thức Chuyên Sâu (Deep Dive)

> **Tài liệu tổng hợp kỹ thuật Backend .NET 10** trong dự án GoldWatch. Tập trung chi tiết vào: Controllers, DTOs, Model Binding & Validation, Entity Framework Core, LINQ, và Tích hợp Dịch vụ.

---

## 1. Controllers & API Design trong ASP.NET Core

### 1.1. Khái Niệm Cơ Bản & Attributes
Tất cả các API endpoint được định nghĩa trong `GoldController` kế thừa từ `ControllerBase`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class GoldController : ControllerBase
{
    private readonly IGoldPriceService _goldPriceService;

    public GoldController(IGoldPriceService goldPriceService)
    {
        _goldPriceService = goldPriceService;
    }
    // ...
}
```

- **`ControllerBase` vs `Controller`**: Với Web API không sử dụng Razor View, luôn kế thừa `ControllerBase` để tránh overhead của View engine.
- **`[ApiController]`**: Kích hoạt các tính năng tự động cực kỳ quan trọng:
  - Tự động validate Model: Nếu request body không hợp lệ (vi phạm Data Annotations), framework tự động trả về `400 Bad Request` dạng RFC 7807 (Problem Details) mà không cần lập trình viên phải viết `if (!ModelState.IsValid)`.
  - Tự động suy luận nguồn binding: `[FromBody]` cho đối tượng phức tạp, `[FromRoute]` cho tham số trên URL, `[FromQuery]` cho query parameters.
- **Route Token `api/[controller]`**: Framework tự động lấy tên class bỏ hậu tố "Controller" (`GoldController` -> `api/gold`).

---

### 1.2. Action Results & Chuẩn HTTP Status Code

Một RESTful API chuẩn phải trả về đúng mã trạng thái HTTP:

| Phương thức HTTP | Endpoint | Action Result | Mã HTTP trả về | Ý nghĩa |
|-----------------|----------|---------------|----------------|---------|
| `GET` | `/api/gold` | `Ok(prices)` | `200 OK` | Lấy danh sách thành công, có kèm body. |
| `GET` | `/api/gold/{id}` | `Ok(price)` hoặc `NotFound()` | `200 OK` / `404 Not Found` | Tìm thấy hoặc không tìm thấy bản ghi. |
| `POST` | `/api/gold` | `CreatedAtAction(...)` | `201 Created` | Tạo mới thành công; trả về kèm header `Location: /api/gold/{newId}`. |
| `PUT` | `/api/gold/{id}` | `NoContent()` hoặc `NotFound()` | `204 NoContent` / `404 Not Found` | Cập nhật thành công (không cần trả body) hoặc không tìm thấy ID. |
| `DELETE` | `/api/gold/{id}` | `NoContent()` hoặc `NotFound()` | `204 NoContent` / `404 Not Found` | Xóa thành công hoặc không tìm thấy ID. |
| `POST` | `/api/gold/sync` | `Ok(syncedPrice)` | `200 OK` | Gọi sync dữ liệu và trả về bản ghi mới nhất. |

#### Ví dụ `CreatedAtAction` thực chiến:
```csharp
[HttpPost]
public async Task<ActionResult<Models.GoldPrice>> Create(CreateGoldPriceRequest request)
{
    var goldPrice = await _goldPriceService.CreateAsync(request);

    return CreatedAtAction(
        nameof(GetByID),             // Tên action để xem chi tiết
        new { id = goldPrice.Id },   // Route params
        goldPrice                    // Response body
    );
}
```

---

### 1.3. CancellationToken — Xử Lý Hủy Bỏ Yêu Cầu
Mọi phương thức I/O (gọi HTTP hoặc truy vấn database) trong dự án đều nhận `CancellationToken`:
```csharp
[HttpPost("sync")]
public async Task<ActionResult<Models.GoldPrice>> SyncLatestPrice(
    [FromQuery] string? type,
    CancellationToken cancellationToken)
```
- Khi người dùng đóng trình duyệt hoặc hủy request giữa chừng, ASP.NET Core sẽ kích hoạt token này.
- Token được truyền xuyên suốt qua `Service -> HttpClient -> DbContext.SaveChangesAsync(cancellationToken)`.
- **Lợi ích:** Ngay lập tức ngắt truy vấn mạng và giải phóng luồng (thread), tiết kiệm tài nguyên máy chủ.

---

## 2. DTOs (Data Transfer Objects) & Data Validation

### 2.1. Phân Biệt Model vs DTO
Trong dự án GoldWatch có sự phân định ranh giới rõ ràng:
1. **Domain Model / Entity (`GoldPrice`):**
   ```csharp
   public class GoldPrice
   {
       public int Id { get; set; }    
       public string GoldType { get; set; } = string.Empty;
       public decimal BuyPrice { get; set; }
       public decimal SellPrice { get; set; }
       public string Currency { get; set; } = "VND";
       public string Source { get; set; } = string.Empty;
       public DateTime CollectedAt { get; set; }
   }
   ```
2. **Input DTO (`CreateGoldPriceRequest`):**
   Người dùng không được phép tự truyền `Id` hay `CollectedAt`. Client chỉ gửi những trường cần nhập:
   ```csharp
   public class CreateGoldPriceRequest
   {
       [Required(ErrorMessage = "GoldType cannot be empty")]
       public string GoldType { get; set; } = string.Empty;

       [Range(0.01, double.MaxValue, ErrorMessage = "Price must larger than 0")]
       public decimal BuyPrice { get; set; }

       [Range(0.01, double.MaxValue, ErrorMessage = "Price must larger than 0")]
       public decimal SellPrice { get; set; }

       [Required]
       public string Currency { get; set; } = string.Empty;

       [Required]
       public string Source { get; set; } = string.Empty;
   }
   ```
3. **External Response DTOs (`VangTodayHistoryResponse`):**
   Sử dụng attribute `[JsonPropertyName]` của `System.Text.Json` để khớp với quy ước đặt tên (naming convention) dạng snake_case hoặc camelCase của API bên ngoài mà không làm hỏng quy tắc đặt tên PascalCase của C#:
   ```csharp
   public class VangTodayHistoryResponse
   {
       [JsonPropertyName("success")]
       public bool Success { get; set; }

       [JsonPropertyName("days")]
       public int Days { get; set; }

       [JsonPropertyName("type")]
       public string Type { get; set; } = string.Empty;

       [JsonPropertyName("history")]
       public List<VangTodayHistoryDateItem> History { get; set; } = [];
   }
   ```

---

## 3. Entity Framework Core (EF Core) & PostgreSQL (Npgsql)

### 3.1. DbContext & Cấu Hình
- `ApplicationDbContext` là đơn vị công việc (Unit of Work) đại diện cho phiên làm việc với database:
  ```csharp
  public class ApplicationDbContext : DbContext
  {
      public ApplicationDbContext(DbContextOptions<ApplicationDbContext> option)
          : base(option) { }

      public DbSet<GoldPrice> GoldPrices => Set<GoldPrice>();
  }
  ```
- Đăng ký Provider PostgreSQL trong `Program.cs`:
  ```csharp
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseNpgsql(connectionString));
  ```

---

### 3.2. LINQ Truy Vấn Nâng Cao & Tối Ưu Hiệu Năng

#### Kỹ thuật `AsNoTracking()`
```csharp
public async Task<IReadOnlyList<GoldPrice>> GetAllAsync()
{
    return await _context.GoldPrices.AsNoTracking().ToListAsync();
}
```
- Mặc định EF Core theo dõi trạng thái thay đổi của mọi entity (`ChangeTracker`).
- Khi chỉ đọc (`GET`), bật `AsNoTracking()` giúp:
  - Giảm đáng kể dung lượng RAM cấp phát.
  - Tăng tốc độ thực thi truy vấn lên đến 30 - 40%.

#### Kỹ thuật Batch Deduplication trong `SyncPriceHistoryAsync`:
Thay vì duyệt vòng lặp rồi query DB từng lần (gây ra vấn đề N+1 queries):
```csharp
// 1. Thu thập danh sách ngày và loại vàng trong bộ nhớ
var datesToCheck = historyPrices.Select(p => p.CollectedAt).ToList();
var typesToCheck = historyPrices.Select(p => p.GoldType).Distinct().ToList();

// 2. Gửi duy nhất 1 câu SQL SELECT có mệnh đề IN
var existingDates = await _context.GoldPrices
    .Where(x => typesToCheck.Contains(x.GoldType)
             && x.Source == "vang.today"
             && datesToCheck.Contains(x.CollectedAt))
    .Select(x => x.CollectedAt)
    .ToListAsync(cancellationToken);

// 3. Lọc trong RAM bằng LINQ to Objects
var newPrices = historyPrices.Where(p => !existingDates.Contains(p.CollectedAt)).ToList();

// 4. Batch Insert
if (newPrices.Count > 0)
{
    _context.GoldPrices.AddRange(newPrices);
    await _context.SaveChangesAsync(cancellationToken);
}
```

---

### 3.3. Xử Lý Ràng Buộc Cơ Sở Dữ Liệu: Unique Constraint (Postgres Error 23505)
- Trong PostgreSQL, bảng `GoldPrices` có unique index:
  `IX_GoldPrices_GoldType_Source_CollectedAt` trên bộ ba `(GoldType, Source, CollectedAt)`.
- Khi cố tình chèn bản ghi trùng lặp, Npgsql sẽ ném ngoại lệ:
  `PostgresException: 23505: duplicate key value violates unique constraint`.
- **Giải pháp trong code:** Luôn thực hiện kiểm tra tồn tại (Pre-check) hoặc Deduplication logic trước khi gọi `.Add()` / `.AddRange()`.

---

### 3.4. Lưu Ý Về Kiểu Thời Gian `DateTime` với PostgreSQL
PostgreSQL kiểu `timestamp with time zone` (TIMESTAMPTZ) yêu cầu đối tượng `DateTime` trong C# phải có `Kind` là `DateTimeKind.Utc`.
Nếu parse chuỗi ngày tháng thông thường (`DateTime.Parse("2026-11-23")`), `Kind` mặc định là `Unspecified` và Npgsql sẽ ném ngoại lệ runtime.
👉 **Giải pháp áp dụng:**
```csharp
DateTime.SpecifyKind(DateTime.Parse(item.Date), DateTimeKind.Utc)
```

---

## 4. Tích Hợp HttpClient với Typed Client Pattern

Trong `Program.cs`, thay vì tạo `new HttpClient()` thủ công:
```csharp
builder.Services.AddHttpClient<IGoldPriceProvider, VangTodayGoldPriceProvider>(client =>
{
    var baseUrl = builder.Configuration["GoldPriceApi:BaseUrl"] ?? "https://www.vang.today/api/";
    client.BaseAddress = new Uri(baseUrl);

    var timeoutSeconds = builder.Configuration.GetValue<int>("GoldPriceApi:TimeoutSeconds", 10);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
```

**Tại sao Typed Client là Best Practice?**
1. **Quản lý Socket hiệu quả:** Tránh lỗi cạn kiệt cổng kết nối (Socket Exhaustion) do `HttpClientHandler` được quản lý bởi `IHttpClientFactory`.
2. **Cấu hình tập trung:** Base URL, Timeout, Default Headers được gom tại một chỗ.
3. **Dễ viết Unit Test:** Dễ dàng mock interface `IGoldPriceProvider` khi test `GoldPriceService`.

