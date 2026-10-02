# V1 — Basic ASP.NET Core Web API

Ở V1, ta sẽ xây một API quản lý **gold price data**, nhưng dữ liệu vẫn nằm trong memory. Chưa có PostgreSQL/EF Core.

Microsoft hiện hỗ trợ cả Minimal APIs và controller-based APIs; với GoldWatch, mình chọn **controller-based API** vì mục tiêu của project là học sâu ASP.NET Core backend, controller, DI, routing và service layer. ([Microsoft Learn][1])

## V1 Goal

Kết thúc V1, architecture sẽ là:

```text
HTTP Request
     │
     ▼
┌───────────────┐
│   Controller  │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│    Service    │
└───────┬───────┘
        │
        ▼
┌────────────────────┐
│ In-memory List<>   │
└────────────────────┘
```

API sẽ có:

```text
GET    /api/gold
GET    /api/gold/{id}
POST   /api/gold
PUT    /api/gold/{id}
DELETE /api/gold/{id}
```

---

# V1.1 — Tạo Solution

Vì máy hiện tại của bạn đang dùng .NET 10 SDK, ta sẽ target **`net10.0`**.

Mở PowerShell:

```powershell
mkdir GoldWatch
cd GoldWatch

dotnet new sln -n GoldWatch
dotnet new webapi -n GoldWatch.Api --framework net10.0
dotnet sln add GoldWatch.Api/GoldWatch.Api.csproj
```

Sau đó:

```powershell
cd GoldWatch.Api
dotnet run
```

Bạn sẽ thấy ASP.NET Core khởi động và in ra URL kiểu:

```text
https://localhost:xxxx
http://localhost:xxxx
```

Mở URL đó trên browser.

### Kiểm tra

```powershell
dotnet build
```

phải:

```text
Build succeeded.
```

---

# V1.2 — Dọn project template

Template mới của ASP.NET Core có thể tạo sẵn một số file mẫu như `WeatherForecast`.

Ta không cần nó.

Xóa:

```text
WeatherForecast.cs
WeatherForecastController.cs
```

Sau đó structure tối thiểu:

```text
GoldWatch
│
├── GoldWatch.sln
│
└── GoldWatch.Api
    │
    ├── Controllers
    │
    ├── Program.cs
    ├── appsettings.json
    └── GoldWatch.Api.csproj
```

Đừng tạo 10 folder ngay lúc này.

---

# V1.3 — Hiểu `Program.cs`

Mở:

```text
GoldWatch.Api/Program.cs
```

Template .NET 10 có thể trông hơi khác các tutorial .NET 8/9 mà bạn tìm được trên mạng. Đây là lý do mình muốn bạn **đọc code hiện tại thay vì copy tutorial cũ**.

Ý tưởng chính vẫn là:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

`builder` dùng để **configure application**.

Sau đó:

```csharp
builder.Services.AddControllers();
```

đăng ký các service cần thiết để ASP.NET Core sử dụng controller-based API. Khi application chạy, `MapControllers()` sẽ map các route của controller vào request pipeline. ([Microsoft Learn][2])

Sau đó:

```csharp
var app = builder.Build();
```

`app` là web application đã được build.

Và:

```csharp
app.MapControllers();
```

nói với ASP.NET:

> "Các controller của tôi là endpoint của application."

Conceptually:

```text
builder
   │
   │ configuration
   ▼
WebApplication
   │
   │ Build()
   ▼
app
   │
   │ MapControllers()
   ▼
HTTP endpoints
```

---

# V1.4 — Tạo Domain Model

Bây giờ bắt đầu code domain.

Tạo:

```text
GoldWatch.Api
└── Models
    └── GoldPrice.cs
```

`GoldPrice.cs`:

```csharp
namespace GoldWatch.Api.Models;

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

### Tại sao `decimal`?

Đây là một câu hỏi interview khá hay.

Giá vàng là dữ liệu tiền tệ/giá trị tài chính.

Ta **không muốn dùng `double` cho monetary values** nếu cần biểu diễn chính xác theo decimal semantics.

Vì vậy:

```csharp
decimal BuyPrice
decimal SellPrice
```

hợp lý hơn:

```csharp
double BuyPrice
double SellPrice
```

---

# V1.5 — Tạo DTO

Đây là lúc ta bắt đầu phân biệt:

> **Domain Model ≠ API Contract**

Tạo:

```text
Models
    GoldPrice.cs

DTOs
    CreateGoldPriceRequest.cs
    UpdateGoldPriceRequest.cs
```

### CreateGoldPriceRequest

```csharp
namespace GoldWatch.Api.DTOs;

public class CreateGoldPriceRequest
{
    public string GoldType { get; set; } = string.Empty;

    public decimal BuyPrice { get; set; }

    public decimal SellPrice { get; set; }

    public string Currency { get; set; } = "VND";

    public string Source { get; set; } = string.Empty;
}
```

Notice:

`Id` không xuất hiện.

`CollectedAt` cũng không xuất hiện.

Vì client không nên tự quyết định:

```text
ID
Collection timestamp
```

Server sẽ quản lý chúng.

---

# V1.6 — Service

Bây giờ đến phần quan trọng nhất của V1.

Tạo:

```text
Services
├── IGoldPriceService.cs
└── GoldPriceService.cs
```

Interface:

```csharp
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;

namespace GoldWatch.Api.Services;

public interface IGoldPriceService
{
    IReadOnlyList<GoldPrice> GetAll();

    GoldPrice? GetById(int id);

    GoldPrice Create(CreateGoldPriceRequest request);

    bool Update(int id, CreateGoldPriceRequest request);

    bool Delete(int id);
}
```

Implementation:

```csharp
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;

namespace GoldWatch.Api.Services;

public class GoldPriceService : IGoldPriceService
{
    private readonly List<GoldPrice> _goldPrices = new();

    public IReadOnlyList<GoldPrice> GetAll()
    {
        return _goldPrices;
    }

    public GoldPrice? GetById(int id)
    {
        return _goldPrices.FirstOrDefault(x => x.Id == id);
    }

    public GoldPrice Create(CreateGoldPriceRequest request)
    {
        var goldPrice = new GoldPrice
        {
            Id = _goldPrices.Count + 1,
            GoldType = request.GoldType,
            BuyPrice = request.BuyPrice,
            SellPrice = request.SellPrice,
            Currency = request.Currency,
            Source = request.Source,
            CollectedAt = DateTime.UtcNow
        };

        _goldPrices.Add(goldPrice);

        return goldPrice;
    }

    public bool Update(int id, CreateGoldPriceRequest request)
    {
        var goldPrice = GetById(id);

        if (goldPrice is null)
        {
            return false;
        }

        goldPrice.GoldType = request.GoldType;
        goldPrice.BuyPrice = request.BuyPrice;
        goldPrice.SellPrice = request.SellPrice;
        goldPrice.Currency = request.Currency;
        goldPrice.Source = request.Source;

        return true;
    }

    public bool Delete(int id)
    {
        var goldPrice = GetById(id);

        if (goldPrice is null)
        {
            return false;
        }

        _goldPrices.Remove(goldPrice);

        return true;
    }
}
```

---

# Khoan chạy — hãy nhìn architecture trước

Ta vừa tạo:

```text
Controller
     ↓
Service
     ↓
List<GoldPrice>
```

Nhưng **chưa có Controller**.

Tại sao?

Vì mình muốn bạn thấy rõ một nguyên tắc:

> Controller không nên chứa business logic.

Sai:

```csharp
[HttpPost]
public IActionResult Create(...)
{
    // validate
    // create object
    // calculate
    // save
    // ...
}
```

Mà:

```text
Controller
    ↓
Service
    ↓
Business logic
```

Controller chỉ chịu trách nhiệm HTTP boundary.

---

# V1.7 — Dependency Injection

Đây là một trong những concept quan trọng nhất của ASP.NET Core.

Trong `Program.cs`, thêm:

```csharp
builder.Services.AddSingleton<IGoldPriceService, GoldPriceService>();
```

ASP.NET Core có built-in Dependency Injection container và controller có thể nhận dependency thông qua constructor injection. ([Microsoft Learn][3])

Architecture:

```text
Program.cs
    │
    │ register
    ▼
IGoldPriceService
       │
       │ implemented by
       ▼
GoldPriceService
```

Khi Controller yêu cầu:

```csharp
IGoldPriceService
```

ASP.NET Core sẽ tìm implementation:

```text
GoldPriceService
```

và inject nó.

---

# V1.8 — Controller

Tạo:

```text
Controllers
└── GoldController.cs
```

```csharp
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoldController : ControllerBase
{
    private readonly IGoldPriceService _goldPriceService;

    public GoldController(IGoldPriceService goldPriceService)
    {
        _goldPriceService = goldPriceService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<Models.GoldPrice>> GetAll()
    {
        return Ok(_goldPriceService.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Models.GoldPrice> GetById(int id)
    {
        var goldPrice = _goldPriceService.GetById(id);

        if (goldPrice is null)
        {
            return NotFound();
        }

        return Ok(goldPrice);
    }

    [HttpPost]
    public ActionResult<Models.GoldPrice> Create(
        CreateGoldPriceRequest request)
    {
        var goldPrice = _goldPriceService.Create(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = goldPrice.Id },
            goldPrice);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(
        int id,
        CreateGoldPriceRequest request)
    {
        var updated = _goldPriceService.Update(id, request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var deleted = _goldPriceService.Delete(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
```

ASP.NET Core API controllers conventionally derive from `ControllerBase`; `[ApiController]` also enables API-specific behaviors such as automatic validation responses and binding inference. ([Microsoft Learn][4])

---

# V1.9 — Test

Chạy:

```powershell
dotnet run
```

Sau đó test:

### GET

```http
GET /api/gold
```

Ban đầu:

```json
[]
```

### POST

```http
POST /api/gold
Content-Type: application/json
```

Body:

```json
{
  "goldType": "SJC",
  "buyPrice": 14230000,
  "sellPrice": 14450000,
  "currency": "VND",
  "source": "Manual"
}
```

Response:

```json
{
  "id": 1,
  "goldType": "SJC",
  "buyPrice": 14230000,
  "sellPrice": 14450000,
  "currency": "VND",
  "source": "Manual",
  "collectedAt": "..."
}
```

Sau đó:

```http
GET /api/gold
```

sẽ trả record vừa tạo.

---

# V1.10 — HTTP Status Codes

Đừng xem đây là syntax phụ.

Bạn cần hiểu tại sao chúng ta return:

```csharp
return Ok(...);
```

→ `200 OK`

```csharp
return CreatedAtAction(...);
```

→ `201 Created`

```csharp
return NoContent();
```

→ `204 No Content`

```csharp
return NotFound();
```

→ `404 Not Found`

Đây là kiến thức REST API cơ bản nhưng rất dễ bị hỏi khi interview.

---

# Một điểm cần sửa nhỏ trong code

Ở `Update`, hiện tại ta đang dùng:

```csharp
CreateGoldPriceRequest
```

cho update.

V1 có thể chấp nhận để tránh tạo quá nhiều abstraction.

Nhưng về sau ta sẽ tách:

```text
CreateGoldPriceRequest
UpdateGoldPriceRequest
GoldPriceResponse
```

Khi V2 có database, ta sẽ refactor lại cho sạch hơn.

---

# V1.11 — Swagger / OpenAPI

Project template .NET 10 hiện dùng OpenAPI tooling; Microsoft cũng có hướng dẫn controller API với OpenAPI/Swagger UI. Swagger UI cho phép bạn khám phá và test endpoint trực tiếp từ browser. ([Microsoft Learn][5])

Nếu template của bạn đã có OpenAPI configuration, hãy **giữ nguyên phần đó** thay vì cài thêm package Swagger cũ chỉ vì tutorial trên mạng dùng:

```csharp
AddSwaggerGen()
```

Sau khi chạy app, kiểm tra endpoint OpenAPI/Swagger mà template của bạn cung cấp.

Điểm quan trọng ở đây là:

```text
OpenAPI
   ↓
API specification

Swagger UI
   ↓
Human-friendly API testing UI
```

---

# V1.12 — Flow hoàn chỉnh

Sau V1, một request sẽ đi như sau:

```text
                    HTTP
                     │
                     ▼
            ┌─────────────────┐
            │ GoldController  │
            └────────┬────────┘
                     │
                     │ IGoldPriceService
                     ▼
            ┌─────────────────┐
            │ GoldPriceService│
            └────────┬────────┘
                     │
                     ▼
              List<GoldPrice>
```

Ví dụ:

```text
POST /api/gold
```

↓

```text
GoldController.Create()
```

↓

```text
GoldPriceService.Create()
```

↓

```text
new GoldPrice(...)
```

↓

```text
_goldPrices.Add(...)
```

↓

```text
201 Created
```

---

# V1.13 — Bạn vừa học những gì?

Nếu map với SchoolCRUDConsole:

| SchoolCRUD             | GoldWatch            |
| ---------------------- | -------------------- |
| `Person` / `Student`   | `GoldPrice`          |
| Interface              | `IGoldPriceService`  |
| Service                | `GoldPriceService`   |
| CRUD methods           | HTTP endpoints       |
| Console input          | HTTP request         |
| Console output         | HTTP response        |
| `List<T>`              | In-memory data       |
| `base` / inheritance   | `ControllerBase`     |
| Manual object creation | Dependency Injection |
| Program entry          | `Program.cs`         |

Nhưng có **một khác biệt rất lớn**:

SchoolCRUD:

```text
User
 ↓
Console
 ↓
Program
 ↓
Service
```

GoldWatch:

```text
Client
 ↓
HTTP
 ↓
ASP.NET Middleware
 ↓
Controller
 ↓
Service
 ↓
Data
```

Đây chính là bước chuyển từ **C# application** sang **web backend application**.

---

# V1 Definition of Done

Đừng sang V2 cho tới khi bạn có thể tick hết:

```text
V1
├── [ ] GoldWatch.sln
├── [ ] GoldWatch.Api
├── [ ] ASP.NET Core Web API chạy được
├── [ ] Program.cs hiểu được cơ bản
├── [ ] GoldPrice model
├── [ ] DTO
├── [ ] IGoldPriceService
├── [ ] GoldPriceService
├── [ ] Dependency Injection
├── [ ] GoldController
├── [ ] GET all
├── [ ] GET by id
├── [ ] POST
├── [ ] PUT
├── [ ] DELETE
├── [ ] HTTP status codes
├── [ ] OpenAPI / Swagger UI
└── [ ] README mô tả API
```

### Và quan trọng nhất — 5 câu bạn phải tự trả lời được

1. **Tại sao không viết toàn bộ logic trong Controller?**
2. **`IGoldPriceService` để làm gì nếu chỉ có một `GoldPriceService`?**
3. **ASP.NET Core biết phải inject `GoldPriceService` vào `GoldController` bằng cách nào?**
4. **`ControllerBase` khác gì một class C# bình thường?**
5. **Tại sao `POST` trả `201 Created` thay vì `200 OK`?**

Nếu bạn tự giải thích được 5 câu này thì V1 đã thực sự có ý nghĩa, chứ không chỉ là "làm được một CRUD API".

**Lưu ý:** ở V1 mình cố tình chưa cho bạn `async Task` vào mọi method. Database/external HTTP/LLM mới là những chỗ async thực sự có ý nghĩa; V2–V3 chúng ta sẽ chuyển dần sang async khi có I/O. Điều này cũng giúp bạn phân biệt rõ `Task` với `async Task` thay vì biến `async` thành một từ khóa phải thêm vào mọi method.

[1]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0&utm_source=chatgpt.com "APIs overview | Microsoft Learn"
[2]: https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app?utm_source=chatgpt.com "Tutorial: Build and secure an ASP.NET Core web API with the Microsoft identity platform - Microsoft identity platform | Microsoft Learn"
[3]: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/dependency-injection?view=aspnetcore-10.0&utm_source=chatgpt.com "Dependency injection into controllers in ASP.NET Core | Microsoft Learn"
[4]: https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0&utm_source=chatgpt.com "Create web APIs with ASP.NET Core | Microsoft Learn"
[5]: https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0&utm_source=chatgpt.com "Tutorial: Create a controller-based web API with ASP.NET Core | Microsoft Learn"
