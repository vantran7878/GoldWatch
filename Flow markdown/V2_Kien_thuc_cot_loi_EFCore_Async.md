# Tổng Hợp Kiến Thức & Syntax Cốt Lõi Version 2 (EF Core, PostgreSQL & Async/Await)

Tài liệu này tổng hợp toàn bộ lý thuyết, nguyên lý kiến trúc và các cú pháp C# / EF Core quan trọng nhất bạn đã áp dụng trong **Version 2**.

---

## MỤC LỤC
1. [Bức Tranh Tổng Thể: Bước Chuyển V1 $\rightarrow$ V2](#1-bức-tranh-tổng-thể-bước-chuyển-v1--v2)
2. [Entity Framework Core (EF Core) & PostgreSQL](#2-entity-framework-core-ef-core--postgresql)
3. [Lập Trình Bất Đồng Bộ (`async` / `await` & `Task`)](#3-lập-trình-bất-đồng-bộ-async--await--task)
4. [Các Cú Pháp EF Core "Sống Còn" Trong Service](#4-các-cú-pháp-ef-core-sống-còn-trong-service)
5. [Cú Pháp Async Trong Controller](#5-cú-pháp-async-trong-controller)
6. [Bảng Tra Cứu Nhanh Syntax & Câu Hỏi Phỏng Vấn V2](#6-bảng-tra-cứu-nhanh-syntax--câu-hỏi-phỏng-vấn-v2)

---

## 1. Bức Tranh Tổng Thể: Bước Chuyển V1 $\rightarrow$ V2

Ở V1, dữ liệu nằm trên RAM (`List<GoldPrice>`). Toàn bộ luồng là **đồng bộ (synchronous)** và **mất dữ liệu khi tắt app**.  
Ở V2, dữ liệu được chuyển xuống ổ cứng thông qua **Hệ quản trị CSDL quan hệ (PostgreSQL)** với sự hỗ trợ của **ORM (Entity Framework Core)**.

```text
               VERSION 1 (In-Memory)                       VERSION 2 (EF Core + DB)
          ┌───────────────────────────────┐           ┌───────────────────────────────┐
          │  Controller (Synchronous)     │           │  Controller (Async / Task)    │
          └──────────────┬────────────────┘           └──────────────┬────────────────┘
                         │                                           │ await
                         ▼                                           ▼
          ┌───────────────────────────────┐           ┌───────────────────────────────┐
          │  Service (Synchronous)        │           │  Service (Async / Task)       │
          └──────────────┬────────────────┘           └──────────────┬────────────────┘
                         │                                           │ await (I/O)
                         ▼                                           ▼
          ┌───────────────────────────────┐           ┌───────────────────────────────┐
          │  RAM: List<GoldPrice>         │           │  ApplicationDbContext         │
          │  (Dữ liệu mất khi tắt app)    │           └──────────────┬────────────────┘
          └───────────────────────────────┘                          │ SQL qua mạng
                                                                     ▼
                                                      ┌───────────────────────────────┐
                                                      │  PostgreSQL (Database Thật)   │
                                                      │  (Dữ liệu tồn tại vĩnh viễn)  │
                                                      └───────────────────────────────┘
```

---

## 2. Entity Framework Core (EF Core) & PostgreSQL

### 2.1. ORM (Object-Relational Mapping) là gì?
EF Core là một **ORM**: Nó đóng vai trò làm "cầu nối" phiên dịch giữa:
* Thế giới hướng đối tượng C# (Class, Object, Property, `List<T>`).
* Thế giới cơ sở dữ liệu quan hệ SQL (Table, Row, Column, Foreign Key).
Bạn thao tác hoàn toàn bằng C#, EF Core sẽ tự sinh ra các câu lệnh SQL (`SELECT`, `INSERT`, `UPDATE`, `DELETE`) gửi đến PostgreSQL.

### 2.2. `ApplicationDbContext` & `DbSet<T>`
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<GoldPrice> GoldPrices => Set<GoldPrice>();
}
```
* **`DbContext`**: Đại diện cho 1 phiên làm việc với database (Unit of Work). Nó quản lý kết nối và theo dõi sự thay đổi của dữ liệu (Change Tracker).
* **`DbSet<GoldPrice>`**: Đại diện cho bảng `GoldPrices` trong Database. Mọi câu query LINQ đều bắt đầu từ `DbSet` này.

### 2.3. Cấu hình Connection String & DI
Trong `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=goldwatch;Username=goldwatch;Password=goldwatch_dev"
}
```
Trong `Program.cs`:
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// BẮT BUỘC dùng Scoped vì DbContext là Scoped (Tránh Captive Dependency)
builder.Services.AddScoped<IGoldPriceService, GoldPriceService>();
```

### 2.4. Công cụ Migrations (Code-First)
* `dotnet ef migrations add <Tên>`: Phân tích class C# và sinh file migration (gồm hàm `Up()` nâng cấp và `Down()` rollback).
* `dotnet ef database update`: Thực thi mã SQL vào PostgreSQL và lưu dấu vết vào bảng `__EFMigrationsHistory`.

---

## 3. Lập Trình Bất Đồng Bộ (`async` / `await` & `Task`)

### 3.1. Tại sao Web API bắt buộc phải dùng `async`?
* Khi server gọi database: PostgreSQL mất vài mili-giây (hoặc vài giây) để tìm kiếm và trả kết quả qua đường truyền mạng. Đây gọi là tác vụ **I/O-bound**.
* **Nếu chạy đồng bộ (Sync):** Luồng xử lý của server (Thread) sẽ bị "treo" (Block), ngồi không chờ database trả về. Khi có hàng nghìn người cùng gửi request, server sẽ cạn kiệt luồng (Thread Starvation) và crash.
* **Khi chạy bất đồng bộ (Async):** Khi gặp từ khóa `await`, Thread hiện tại được giải phóng trả về Thread Pool để đi phục vụ request của người khác. Khi database trả kết quả về xong, một thread rảnh rỗi sẽ quay lại tiếp tục chạy nốt phần code phía sau. $\rightarrow$ **Tăng khả năng chịu tải (Scalability) của hệ thống lên gấp nhiều lần.**

### 3.2. Ba quy tắc cú pháp khi viết Async trong C#
1. **Từ khóa `async`**: Đặt trước khai báo hàm để báo cho trình biên dịch biết hàm này có chứa `await`.
2. **Kiểu trả về `Task` hoặc `Task<T>`**:
   * Hàm không trả dữ liệu (tương đương `void`): Dùng `Task`.
   * Hàm có trả dữ liệu kiểu `T`: Dùng `Task<T>`.
3. **Từ khóa `await`**: Đặt trước phương thức bất đồng bộ để chờ lấy kết quả thực sự.
   * `Task<GoldPrice> task = ...` $\rightarrow$ Chưa lấy được giá trị, chỉ là đại diện cho tác vụ đang chạy.
   * `GoldPrice price = await ...` $\rightarrow$ Đã "mở gói" `Task` và lấy ra đối tượng `GoldPrice` bên trong.
4. **Hậu tố `Async`**: Quy ước đặt tên hàm (ví dụ: `GetAllAsync`, `CreateAsync`, `SaveChangesAsync`).

---

## 4. Các Cú Pháp EF Core "Sống Còn" Trong Service

Dưới đây là 5 thao tác CRUD cơ bản nhất bằng EF Core:

### 4.1. Đọc danh sách (`GetAllAsync`) & Kỹ thuật `.AsNoTracking()`
```csharp
public async Task<IReadOnlyList<GoldPrice>> GetAllAsync()
{
    return await _context.GoldPrices
        .AsNoTracking()
        .ToListAsync();
}
```
* **`.AsNoTracking()`**: Báo cho EF Core tắt bộ nhớ theo dõi (Change Tracker). Dùng cho các thao tác **chỉ đọc (Read-only)** giúp tăng tốc độ xử lý và tiết kiệm RAM.
* **`.ToListAsync()`**: Thực thi câu lệnh `SELECT * FROM "GoldPrices"` gửi xuống PostgreSQL và chuyển kết quả thành danh sách C#.

### 4.2. Đọc theo ID (`GetByIdAsync`)
```csharp
public async Task<GoldPrice?> GetByIDAsync(int id)
{
    return await _context.GoldPrices
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);
}
```
* **`.FirstOrDefaultAsync(lambda)`**: Dịch thành `SELECT * FROM "GoldPrices" WHERE "Id" = @id LIMIT 1`. Nếu không tìm thấy, trả về `null`.

### 4.3. Thêm mới (`CreateAsync`) & Cơ chế tự sinh ID
```csharp
public async Task<GoldPrice> CreateAsync(CreateGoldPriceRequest request)
{
    var goldPrice = new GoldPrice
    {
        GoldType = request.GoldType,
        BuyPrice = request.BuyPrice,
        SellPrice = request.SellPrice,
        Currency = request.Currency,
        Source = request.Source,
        CollectedAt = DateTime.UtcNow
    };

    _context.GoldPrices.Add(goldPrice); // Đánh dấu Entity ở trạng thái 'Added'
    await _context.SaveChangesAsync();  // Dịch thành INSERT INTO... và thực thi

    return goldPrice; // Sau SaveChangesAsync, goldPrice.Id đã tự động có giá trị mới từ DB
}
```
* `Add()`: Chỉ đánh dấu đối tượng vào bộ nhớ của EF Core, **chưa có câu lệnh SQL nào gửi đi**.
* `SaveChangesAsync()`: Gửi lệnh `INSERT INTO "GoldPrices" ... RETURNING "Id"` xuống database. PostgreSQL tự cấp phát ID và EF Core gán ngược lại vào property `goldPrice.Id`.

### 4.4. Cập nhật (`UpdateAsync`) & Change Tracking (Theo dõi thay đổi)
```csharp
public async Task<bool> UpdateAsync(int id, CreateGoldPriceRequest request)
{
    // BƯỚC 1: Query đối tượng CÓ THEO DÕI (KHÔNG dùng AsNoTracking)
    var goldPrice = await _context.GoldPrices.FirstOrDefaultAsync(x => x.Id == id);

    if (goldPrice is null)
    {
        return false;
    }

    // BƯỚC 2: Sửa các thuộc tính trên Entity
    goldPrice.GoldType = request.GoldType;
    goldPrice.BuyPrice = request.BuyPrice;
    goldPrice.SellPrice = request.SellPrice;
    goldPrice.Currency = request.Currency;
    goldPrice.Source = request.Source;

    // BƯỚC 3: Lưu lại
    await _context.SaveChangesAsync();
    return true;
}
```
* **Nguyên lý Dirty Checking:** Khi bạn sửa giá trị của `goldPrice`, EF Core so sánh đối tượng hiện tại với bản chụp snapshot ban đầu của nó.
* Khi gọi `SaveChangesAsync()`, EF Core biết chính xác property nào đã bị đổi và tự sinh ra lệnh:
  `UPDATE "GoldPrices" SET "GoldType" = ..., "BuyPrice" = ... WHERE "Id" = @id`.

### 4.5. Xóa (`DeleteAsync`)
```csharp
public async Task<bool> DeleteAsync(int id)
{
    var goldPrice = await _context.GoldPrices.FirstOrDefaultAsync(x => x.Id == id);

    if (goldPrice is null)
    {
        return false;
    }

    _context.GoldPrices.Remove(goldPrice); // Đánh dấu trạng thái 'Deleted'
    await _context.SaveChangesAsync();     // Dịch thành DELETE FROM "GoldPrices" WHERE "Id" = @id
    return true;
}
```

---

## 5. Cú Pháp Async Trong Controller

Mọi Action Method trong Controller khi gọi Service bất đồng bộ đều phải đổi sang `async Task<...>`:

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

    // 1. GET ALL: Trả về Task<ActionResult<...>>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoldPrice>>> GetAll()
    {
        var prices = await _goldPriceService.GetAllAsync();
        return Ok(prices);
    }

    // 2. GET BY ID: Task<ActionResult<GoldPrice>>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoldPrice>> GetById(int id)
    {
        var price = await _goldPriceService.GetByIDAsync(id);
        if (price is null) return NotFound();
        return Ok(price);
    }

    // 3. POST: CreatedAtAction
    [HttpPost]
    public async Task<ActionResult<GoldPrice>> Create(CreateGoldPriceRequest request)
    {
        var created = await _goldPriceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // 4. PUT: Trả về Task<IActionResult> (NoContent)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateGoldPriceRequest request)
    {
        var updated = await _goldPriceService.UpdateAsync(id, request);
        if (!updated) return NotFound();
        return NoContent();
    }

    // 5. DELETE: Trả về Task<IActionResult> (NoContent)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _goldPriceService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
```

---

## 6. Bảng Tra Cứu Nhanh Syntax & Câu Hỏi Phỏng Vấn V2

### 6.1. Bảng tra cứu các phương thức EF Core

| Cú pháp EF Core | Ý nghĩa | Dịch sang SQL tương đương |
| :--- | :--- | :--- |
| `_context.Set.ToListAsync()` | Lấy toàn bộ danh sách bản ghi | `SELECT * FROM Table` |
| `_context.Set.FirstOrDefaultAsync(x => ...)` | Lấy bản ghi đầu tiên khớp điều kiện | `SELECT * FROM Table WHERE ... LIMIT 1` |
| `.AsNoTracking()` | Không theo dõi thay đổi (tối ưu Read) | *(Tối ưu nội bộ trong RAM của ứng dụng)* |
| `_context.Set.Add(entity)` | Đánh dấu entity là bản ghi mới | Đợi đến `SaveChangesAsync` mới `INSERT` |
| `_context.Set.Remove(entity)` | Đánh dấu entity cần bị xóa | Đợi đến `SaveChangesAsync` mới `DELETE` |
| `await _context.SaveChangesAsync()` | Lưu tất cả thay đổi xuống DB | Bắt đầu Transaction, thực thi các lệnh SQL |

### 6.2. Câu hỏi phỏng vấn trọng tâm

1. **Sự khác nhau giữa `IEnumerable` và `IQueryable` trong EF Core?**
   * `IEnumerable`: Thực thi query trên memory (RAM).
   * `IQueryable`: Chưa thực thi ngay, cho phép nối thêm câu lệnh (filter, sorting, paging). Khi nào gọi `ToListAsync()`, nó mới dịch toàn bộ thành một câu lệnh SQL duy nhất gửi xuống Database.
2. **Tại sao nên dùng `AsNoTracking()`?**
   * Giúp ứng dụng chạy nhanh hơn và ngốn ít RAM hơn vì EF Core không phải khởi tạo đối tượng Change Tracker để theo dõi entity.
3. **`SaveChangesAsync()` hoạt động theo cơ chế nào?**
   * Nó thực thi theo mô hình **Unit of Work**: Gom tất cả các thao tác (Thêm, Sửa, Xóa) vào một **Database Transaction duy nhất**. Nếu 1 câu lệnh bị lỗi, toàn bộ transaction sẽ được Rollback tự động để bảo toàn tính toàn vẹn dữ liệu.
4. **Tại sao `DbContext` phải đăng ký Scoped thay vì Singleton?**
   * Vì `DbContext` không thread-safe, lưu cache trong Change Tracker dễ gây rò rỉ bộ nhớ, và mỗi HTTP Request cần là một phiên làm việc độc lập.
