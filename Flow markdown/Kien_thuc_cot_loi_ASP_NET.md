# Kiến Thức Cốt Lõi ASP.NET Core Web API (Dành Cho Người Mới)

Tài liệu này tổng hợp toàn bộ các khái niệm nền tảng trong ASP.NET Core mà bạn đã áp dụng trong **Version 1 (V1)** của dự án **GoldWatch**.

---

## Mục Lục
1. [WebApplicationBuilder (`builder`) là gì?](#1-webapplicationbuilder-builder-là-gì)
2. [Dependency Injection (DI) & Cách Inject Service vào Controller](#2-dependency-injection-di--cách-inject-service-vào-controller)
3. [Giải mã toàn bộ luồng chạy của `Program.cs`](#3-giải-mã-toàn-bộ-luồng-chạy-của-programcs)
4. [Cách viết Controller chuẩn trong ASP.NET Core](#4-cách-viết-controller-chuẩn-trong-aspnet-core)
5. [Mô hình tổng thể một Request từ Client đến Service](#5-mô-hình-tổng-thể-một-request)

---

## 1. WebApplicationBuilder (`builder`) là gì?

Ở các ứng dụng Console thông thường, bạn chỉ có một hàm `static void Main(string[] args)`. Mọi thứ bạn tạo ra đều phải tự khởi tạo thủ công bằng từ khóa `new`.

Trong ASP.NET Core, ứng dụng web cần rất nhiều hạ tầng phức tạp:
* Web server (Kestrel) để mở cổng socket lắng nghe kết nối mạng (HTTP/HTTPS).
* Bộ đọc file cấu hình (`appsettings.json`, biến môi trường...).
* Bộ ghi log (Logging).
* Thùng chứa phụ thuộc (IoC/DI Container).

`WebApplication.CreateBuilder(args)` chính là đối tượng giúp bạn gom tất cả các hạ tầng này lại để chuẩn bị dựng nên ứng dụng.

### Quy luật 2 pha bất biến trong `Program.cs`

ASP.NET Core chia việc khởi động ứng dụng làm **2 pha rõ rệt**:

```text
       PHA 1: CẤU HÌNH DỊCH VỤ (Service Registration)
  builder.Services.Add...()  ->  Đăng ký các "nguyên vật liệu" vào thùng DI
                      │
                      ▼
               builder.Build()    ->  ĐÓNG BĂNG & XÂY DỰNG ỨNG DỤNG
                      │
                      ▼
       PHA 2: CẤU HÌNH ĐƯỜNG ỐNG (Middleware Pipeline)
  app.Use...() / app.Map...() ->  Quy định cách xử lý một HTTP Request đi qua
                      │
                      ▼
                  app.Run()      ->  CHÍNH THỨC LẮNG NGHE KẾT NỐI
```

> **Lưu ý cốt lõi:** Sau khi dòng `var app = builder.Build();` được gọi, `builder.Services` sẽ bị khóa lại thành **Read-only**. Bạn không thể gọi `builder.Services.Add...` sau thời điểm này (đây chính là nguyên nhân gây lỗi crash `InvalidOperationException` ban đầu).

---

## 2. Dependency Injection (DI) & Cách Inject Service vào Controller

### 2.1. Tại sao không dùng `new` trực tiếp trong Controller?

Nếu trong Controller bạn viết:
```csharp
// CÁCH CŨ (Anti-pattern):
public class GoldController : ControllerBase
{
    private readonly GoldPriceService _goldPriceService = new GoldPriceService();
}
```
Hậu quả:
1. **Dính chặt (Tight coupling):** `GoldController` phụ thuộc hoàn toàn vào class cụ thể `GoldPriceService`. Mai sau ở V2 muốn đổi sang `PostgresGoldPriceService`, bạn buộc phải vào Controller sửa code.
2. **Không thể Unit Test:** Bạn không thể thay thế một Mock Service giả lập để test Controller độc lập.
3. **Mất kiểm soát vòng đời:** Mỗi lần Controller tạo ra, nó lại `new` một service mới, làm mất toàn bộ danh sách dữ liệu trong bộ nhớ (`_goldPrices`).

### 2.2. Giải pháp: Constructor Injection qua Interface

Thay vì tự `new`, Controller chỉ khai báo: *"Tôi cần một anh nào đó thực hiện hợp đồng `IGoldPriceService`"*:

```csharp
[ApiController]
[Route("api/[controller]")]
public class GoldController : ControllerBase
{
    private readonly IGoldPriceService _goldPriceService;

    // ASP.NET Core sẽ tự động tìm và bơm (inject) instance vào đây
    public GoldController(IGoldPriceService goldPriceService)
    {
        _goldPriceService = goldPriceService;
    }
}
```

### 2.3. Khai báo với ASP.NET Core bằng Service Lifetimes

Trong `Program.cs`, bạn đăng ký:
```csharp
builder.Services.AddSingleton<IGoldPriceService, GoldPriceService>();
```
Ý nghĩa: *"Khi có ai yêu cầu `IGoldPriceService`, hãy cấp phát class `GoldPriceService`"*.

ASP.NET Core hỗ trợ **3 vòng đời (Lifetimes)** cơ bản — đây là câu hỏi phỏng vấn cực kỳ phổ biến:

| Lifetime | Cú pháp | Bản chất hoạt động | Dùng khi nào? |
| :--- | :--- | :--- | :--- |
| **Transient** | `AddTransient<I, T>()` | Mỗi lần cần là tạo một object **mới tinh**. | Các service nhỏ, không giữ trạng thái (stateless), tính toán nhanh. |
| **Scoped** | `AddScoped<I, T>()` | Tạo **1 object duy nhất cho mỗi HTTP Request**. Mọi class trong cùng 1 request dùng chung object này; sang request mới sẽ tạo object khác. | Dùng cho kết nối Database (`DbContext` trong EF Core ở V2), Repository. |
| **Singleton** | `AddSingleton<I, T>()` | Tạo **đúng 1 object duy nhất** trong suốt vòng đời của app từ lúc bật đến lúc tắt. | Dùng cho Cache, in-memory state (như `_goldPrices` ở V1), Background Job coordinator. |

> **Tại sao V1 phải dùng Singleton?**
> Vì V1 lưu dữ liệu giá vàng trong một `List<GoldPrice>` trong RAM. Nếu bạn dùng `Transient` hoặc `Scoped`, mỗi lần bạn gọi `POST` xong sang gọi `GET`, ASP.NET Core sẽ tạo ra một service mới với danh sách `_goldPrices` rỗng!

---

## 3. Giải mã toàn bộ luồng chạy của `Program.cs`

Dưới đây là từng dòng trong file `Program.cs` hiện tại của bạn và vai trò kỹ thuật của chúng:

```csharp
using GoldWatch.Api.Services;

// BƯỚC 1: Khởi tạo Builder
var builder = WebApplication.CreateBuilder(args);

// BƯỚC 2: Đăng ký các Service vào DI Container
// Tạo tài liệu OpenAPI (Swagger chuẩn mới trong .NET 9/10)
builder.Services.AddOpenApi();

// Đăng ký Service nghiệp vụ của chúng ta với vòng đời Singleton
builder.Services.AddSingleton<IGoldPriceService, GoldPriceService>();

// Kích hoạt hạ tầng Controller (Model binding, Filters, JSON formatting, Controller discovery...)
builder.Services.AddControllers();

// BƯỚC 3: Đóng băng cấu hình và sinh ra Web Application instance
var app = builder.Build();

// BƯỚC 4: Cấu hình Middleware Pipeline (Thứ tự request đi vào)
// Nếu đang ở môi trường phát triển (Development) thì mở endpoint xem tài liệu OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Tự động chuyển các request http:// sang https:// an toàn
app.UseHttpsRedirection();

// Báo cho bộ định tuyến (Router) biết quét các Controller để tạo đường dẫn API (/api/gold)
app.MapControllers();

// Kích hoạt máy chủ Kestrel và bắt đầu lắng nghe request
app.Run();
```

### Middleware Pipeline là gì?
Hãy tưởng tượng Request từ phía người dùng giống như nước chảy qua một loạt các bộ lọc (gọi là Middleware):

$$\text{HTTP Request} \longrightarrow [\text{HttpsRedirection}] \longrightarrow [\text{Routing / MapControllers}] \longrightarrow [\text{Controller}] \longrightarrow \text{HTTP Response}$$

* Các method bắt đầu bằng **`Use...`** (như `UseHttpsRedirection`): Tham gia biến đổi hoặc chuyển hướng request.
* Các method bắt đầu bằng **`Map...`** (như `MapControllers`, `MapOpenApi`): Đóng vai trò là đích đến cuối cùng (Endpoint) để sinh dữ liệu trả về cho client.

---

## 4. Cách viết Controller chuẩn trong ASP.NET Core

Cấu trúc chuẩn của một API Controller:

```csharp
namespace GoldWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoldController : ControllerBase
{
    // ...
}
```

### 4.1. Kế thừa `ControllerBase` thay vì `Controller`
* Trong mô hình cũ (ASP.NET Core MVC làm web có giao diện), controller kế thừa `Controller` (hỗ trợ hiển thị giao diện HTML `.cshtml` qua hàm `View()`).
* Đối với **Web API thuần túy** (chỉ trả về JSON/XML), ta kế thừa **`ControllerBase`** để loại bỏ các tính năng dư thừa của giao diện, tối ưu hiệu năng.

### 4.2. Vai trò của Attribute `[ApiController]`
Khi bạn đặt `[ApiController]` lên đầu class, ASP.NET Core tự động kích hoạt 3 tính năng cực mạnh:
1. **Tự động suy luận nguồn dữ liệu (Binding Source Inference):** Nó tự biết tham số phức tạp (như `CreateGoldPriceRequest`) cần lấy từ Body (`[FromBody]`), còn tham số cơ bản (`int id`) lấy từ Route (`[FromRoute]`).
2. **Tự động kiểm tra tính hợp lệ (Automatic Model Validation):** Nếu dữ liệu gửi lên sai định dạng, nó tự động trả về lỗi `400 Bad Request` mà bạn không cần phải tự viết `if (!ModelState.IsValid)`.
3. **Chuẩn hóa lỗi (Problem Details):** Trả về thông báo lỗi theo chuẩn RFC 7807 dễ hiểu cho client.

### 4.3. Route Token `[Route("api/[controller]")]`
Token `[controller]` sẽ tự động lấy tên của Class nhưng **bỏ đi chữ `Controller`**:
* Class `GoldController` $\rightarrow$ Route: `/api/gold`
* Class `NewsController` $\rightarrow$ Route: `/api/news`

### 4.4. Cách viết Action Method và HTTP Status Code chuẩn RESTful

Một Action Method nên trả về `ActionResult<T>` hoặc `IActionResult`:
* `IActionResult`: Dùng khi phương thức chỉ trả về status code không có body dữ liệu (ví dụ `NoContent()`).
* `ActionResult<T>`: Dùng khi phương thức trả về dữ liệu kiểu `T` kèm status code (ví dụ `Ok(goldPrice)`).

Các mã HTTP status code chuẩn:

```csharp
// 1. GET ALL -> Trả về 200 OK kèm danh sách
[HttpGet]
public ActionResult<IReadOnlyList<GoldPrice>> GetAll()
{
    return Ok(_goldPriceService.GetAll()); // HTTP 200
}

// 2. GET BY ID -> Có ràng buộc {id:int}, trả về 200 hoặc 404
[HttpGet("{id:int}")]
public ActionResult<GoldPrice> GetById(int id)
{
    var item = _goldPriceService.GetByID(id);
    if (item is null)
    {
        return NotFound(); // HTTP 404
    }
    return Ok(item); // HTTP 200
}

// 3. POST -> Tạo mới thành công, trả về 201 Created kèm header Location
[HttpPost]
public ActionResult<GoldPrice> Create(CreateGoldPriceRequest request)
{
    var created = _goldPriceService.Create(request);

    // Trả về HTTP 201 Created, kèm Header "Location: /api/gold/{id}" 
    // và Body chính là object vừa tạo
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
}

// 4. PUT -> Cập nhật thành công, trả về 204 No Content hoặc 404
[HttpPut("{id:int}")]
public IActionResult Update(int id, CreateGoldPriceRequest request)
{
    var updated = _goldPriceService.Update(id, request);
    if (!updated)
    {
        return NotFound(); // HTTP 404
    }
    return NoContent(); // HTTP 204 (đã sửa xong, không cần trả dữ liệu gì về)
}

// 5. DELETE -> Xóa thành công, trả về 204 No Content hoặc 404
[HttpDelete("{id:int}")]
public IActionResult Delete(int id)
{
    var deleted = _goldPriceService.Delete(id);
    if (!deleted)
    {
        return NotFound(); // HTTP 404
    }
    return NoContent(); // HTTP 204
}
```

---

## 5. Mô hình tổng thể một Request

Hãy nhìn lại toàn bộ bức tranh cách dữ liệu đi từ trình duyệt/Postman vào đến code của bạn:

```text
1. Client gọi HTTP POST: http://localhost:5000/api/gold
   Body: {"goldType": "SJC", "buyPrice": 80000000, "sellPrice": 82000000, ...}
                     │
                     ▼
2. Kestrel Server tiếp nhận kết nối mạng (app.Run())
                     │
                     ▼
3. Request đi qua Middleware Pipeline (UseHttpsRedirection, ...)
                     │
                     ▼
4. Router (app.MapControllers()) nhận diện:
   - Method: POST
   - URL: /api/gold
   -> Khớp với GoldController.Create(...)
                     │
                     ▼
5. ASP.NET Activator khởi tạo Controller:
   - Nhìn thấy Controller cần IGoldPriceService
   - Lấy instance GoldPriceService từ DI Container (AddSingleton)
   - Bơm vào constructor của GoldController
                     │
                     ▼
6. ASP.NET Model Binder:
   - Đọc JSON từ Body, chuyển thành object CreateGoldPriceRequest
                     │
                     ▼
7. Hàm GoldController.Create(request) chạy:
   - Gọi _goldPriceService.Create(request)
   - Lưu vào List<GoldPrice>
   - Nhận lại GoldPrice mới tạo
                     │
                     ▼
8. Controller trả về: CreatedAtAction(...) -> HTTP 201 Created
   - JSON Serializer tự động biến object GoldPrice thành chuỗi JSON
   - Trả ngược về cho Client
```

---

## 6. Checklist tự ôn tập kiến thức V1

Hãy thử nhẩm lại 5 câu hỏi này, nếu bạn trả lời trôi chảy thì bạn đã hoàn toàn làm chủ kiến thức V1:

1. **`builder.Services` và `app` khác nhau như thế nào về nhiệm vụ và thứ tự xuất hiện?**
2. **Nếu thay `AddSingleton` bằng `AddTransient` cho `IGoldPriceService`, hiện tượng gì sẽ xảy ra khi gọi API liên tục?**
3. **Tại sao hàm `Create` nên trả về `CreatedAtAction` (`201 Created`) thay vì `Ok` (`200 OK`)?**
4. **Tại sao ta không truyền trực tiếp `GoldPrice` (Model) từ Client lên mà phải tạo riêng `CreateGoldPriceRequest` (DTO)?**
5. **Dòng lệnh `app.MapControllers()` có tác dụng gì? Nếu quên không gọi nó thì API sẽ ra sao?**
