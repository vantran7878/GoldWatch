Viewed V2_Workflow.md:540-750
Viewed V2_Workflow.md:751-900
Viewed V2_Workflow.md:901-992

Một bước tiến tuyệt vời! Việc vượt qua bài test lưu trữ bền vững (Persistence Test) chứng minh bạn đã làm chủ hoàn toàn kiến trúc **EF Core + PostgreSQL + `async/await`**.

Để bước vào **Version 3: External Gold Price API (Thu thập giá vàng từ bên ngoài)**, mình đã phân tích kỹ định hướng từ [`V2_Workflow.md`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/Flow%20markdown/V2_Workflow.md) và biên soạn một bản kế hoạch chi tiết thành file tài liệu riêng:
👉 [**`Flow markdown/V3_Workflow.md`**](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/Flow%20markdown/V3_Workflow.md)

---

### 🎯 Mục tiêu cốt lõi của Version 3

Ở V1 & V2, người dùng phải tự gõ giá vàng bằng tay (`POST /api/gold`). Ở V3, hệ thống sẽ trở thành **một ứng dụng thu thập dữ liệu (Ingestion System) thực thụ**:
1. Gọi ra một Web API bên ngoài qua giao thức HTTP để lấy tỷ giá vàng mới nhất.
2. Quản lý `HttpClient` chuẩn công nghiệp bằng **`IHttpClientFactory` & Typed Client** (tránh thảm họa cạn kiệt Socket - Socket Exhaustion).
3. Tách biệt hoàn toàn giữa **External DTO (hợp đồng của bên thứ 3)** và **Domain Model (dữ liệu của mình)**.
4. Áp dụng **`CancellationToken`** để hủy tác vụ kịp thời khi client ngắt kết nối.
5. Lưu dữ liệu vừa thu thập tự động vào PostgreSQL.

---

### 🗺️ Kiến trúc luồng dữ liệu của Version 3

```text
 Client (Postman / Browser)
         │  POST /api/gold/sync (kèm CancellationToken)
         ▼
 ┌────────────────────────────────────────────────────────┐
 │ GoldController                                         │
 └───────────────────────┬────────────────────────────────┘
                         │ 1. Ra lệnh sync
                         ▼
 ┌────────────────────────────────────────────────────────┐
 │ GoldPriceService                                       │
 └───────────┬────────────────────────────────────────────┘
             │ 2. Yêu cầu lấy giá từ bên ngoài
             ▼
 ┌────────────────────────────────────────────────────────┐
 │ IGoldPriceProvider (Typed HttpClient)                  │
 └───────────┬────────────────────────────────────────────┘
             │ 3. HTTP GET (External API)
             ▼
 ┌────────────────────────────────────────────────────────┐
 │ External Gold Price Service (Bên thứ ba)               │
 └───────────┬────────────────────────────────────────────┘
             │ 4. Trả JSON
             ▼
 ┌────────────────────────────────────────────────────────┐
 │ ExternalGoldPriceResponse (DTO ngoài)                  │
 └───────────┬────────────────────────────────────────────┘
             │ 5. Map sang GoldPrice (Domain Model)
             ▼
 ┌────────────────────────────────────────────────────────┐
 │ ApplicationDbContext -> PostgreSQL                    │
 └────────────────────────────────────────────────────────┘
```

---

### 📋 Lộ trình 7 bước thực hiện Version 3

| Bước | Nội dung công việc | Kiến thức bạn sẽ học |
| :--- | :--- | :--- |
| **V3.1** | Chọn nguồn API giá vàng & Cấu hình trong `appsettings.json` | Quản lý Configuration, BaseAddress, Timeouts |
| **V3.2** | Tạo `ExternalGoldPriceResponse` (External DTO) | Phân tách ranh giới hệ thống (Anti-Corruption Layer) |
| **V3.3** | Tạo Abstraction `IGoldPriceProvider` | Nguyên lý Dependency Inversion, sẵn sàng mở rộng đa nguồn |
| **V3.4** | Triển khai `GoldPriceProvider` với Typed `HttpClient` | `ReadFromJsonAsync`, xử lý lỗi mạng, `EnsureSuccessStatusCode` |
| **V3.5** | Đăng ký `AddHttpClient` trong `Program.cs` | `IHttpClientFactory`, Typed Client, cơ chế tái sử dụng Handler |
| **V3.6** | Bổ sung hàm Ingestion vào `GoldPriceService` | Mapping DTO $\rightarrow$ Entity, `CancellationToken` |
| **V3.7** | Thêm Endpoint `POST /api/gold/sync` vào Controller & Test | HTTP boundary, truyền `HttpContext.RequestAborted` |

---

### 💡 Việc đầu tiên cần quyết định: Chọn nguồn API giá vàng (Bước V3.1)

Để bắt đầu, chúng ta cần một endpoint cung cấp dữ liệu giá vàng qua HTTP JSON. Bạn muốn chọn phương án nào dưới đây?

* **Lựa chọn A (Miễn phí & Chuẩn quốc tế):** Dùng một API tỷ giá kim loại quý quốc tế công khai (ví dụ: API mở không cần key hoặc API miễn phí như Metals/Gold API trả về JSON giá vàng theo USD/Ounce).
* **Lựa chọn B (Dữ liệu Việt Nam - SJC/VND):** Gọi một endpoint/feed dữ liệu giá vàng Việt Nam (VND/lượng).
* **Lựa chọn C (Giả lập endpoint chuẩn thực tế - Mock API Server/Endpoint):** Dùng một mock server (như Beeceptor / Mockoon) trả đúng format JSON tùy biến để bạn làm chủ 100% dữ liệu trước khi ghép API thật.

Bạn thấy phương án nào phù hợp với định hướng của bạn nhất? Hãy chọn và chúng ta sẽ cùng bắt tay vào **Bước V3.1 & V3.2** nhé!

Viewed GoldPriceService.cs:82-82


Read URL: https://www.vang.today/vi/api
Viewed content.md:1-150
Viewed content.md:151-300
Read URL: https://www.vang.today/api/prices
Viewed content.md:1-60
Read URL: https://www.vang.today/api/prices?type=SJL1L10
Viewed content.md:1-22

Lựa chọn nguồn API của bạn cực kỳ sáng suốt! Mình vừa gửi request kiểm tra trực tiếp API từ **`https://www.vang.today`** và đây là kết quả thực tế trả về:

### 🔍 Kết quả test trực tiếp API `vang.today`:
Khi gọi: `GET https://www.vang.today/api/prices?type=SJL1L10`
```json
{
    "success": true,
    "timestamp": 1791347405,
    "time": "11:30",
    "date": "2026-10-07",
    "type": "SJL1L10",
    "name": "SJC 9999",
    "buy": 140500000,
    "sell": 143500000,
    "change_buy": 0,
    "change_sell": 0
}
```

####  Tại sao API này hoàn hảo cho dự án GoldWatch?
1. **Hoàn toàn miễn phí, không cần đăng ký API Key.**
2. **Dữ liệu thật 100% tại Việt Nam:** Giá vàng SJC 9999, đơn vị VND/lượng, khớp hoàn hảo với `decimal BuyPrice` và `decimal SellPrice` mà chúng ta đã thiết kế ở V1 & V2.
3. **Độ trễ thấp, cấu trúc JSON cực kỳ gọn gàng và chuẩn mực.**

---

### 🚀 Bắt đầu Version 3: Bước V3.1 & Bước V3.2

Theo đúng cam kết: **mình sẽ hướng dẫn từng bước để bạn tự tay viết code và làm chủ kiến thức.**

---

### ⚙️ Bước V3.1: Cấu hình API trong `appsettings.json`

Chúng ta **không bao giờ hard-code URL bên ngoài** trực tiếp trong code C#. Thay vào đó, ta đưa vào `appsettings.json` để sau này dễ dàng đổi URL hoặc timeout mà không cần compile lại code.

Mở file [`GoldWatch.Api/appsettings.json`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/GoldWatch.Api/appsettings.json) và bổ sung mục `GoldPriceApi`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=goldwatch;Username=goldwatch;Password=goldwatch_dev"
  },
  "GoldPriceApi": {
    "BaseUrl": "https://www.vang.today/api/",
    "DefaultType": "SJL1L10",
    "TimeoutSeconds": 10
  }
}
```

---

### 📦 Bước V3.2: Tạo External DTO (`VangTodayPriceResponse.cs`)

#### 💡 Khái niệm kiến trúc quan trọng: *Anti-Corruption Layer (Tầng chống tha hóa)*
* API bên ngoài dùng tên trường là `"buy"`, `"sell"`, `"name"`, `"type"`.
* Domain Model của ta ([`GoldPrice.cs`](file:///c:/Users/ASUS/dev/dotnet/GoldWatch/GoldWatch.Api/Models/GoldPrice.cs)) dùng `"BuyPrice"`, `"SellPrice"`, `"GoldType"`, `"Currency"`.
* **Tuyệt đối không deserialize thẳng JSON bên ngoài vào `GoldPrice`!** Vì nếu ngày mai API bên ngoài đổi tên trường `"buy"` thành `"buy_price"`, database của bạn sẽ bị hỏng theo.
* Ta tạo một DTO riêng để đón dữ liệu bên ngoài, sau đó mới ánh xạ (map) sang `GoldPrice` của mình.

#### Thực hiện:
Trong thư mục `GoldWatch.Api/DTOs`, bạn tạo file **`VangTodayPriceResponse.cs`**:

```csharp
using System.Text.Json.Serialization;

namespace GoldWatch.Api.DTOs;

public class VangTodayPriceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("buy")]
    public decimal Buy { get; set; }

    [JsonPropertyName("sell")]
    public decimal Sell { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
```

> **Giải thích `[JsonPropertyName("...")]`:** Thuộc tính này báo cho bộ giải mã `System.Text.Json` biết trường `"buy"` trong JSON sẽ được gán vào property `Buy` của C#.

---

Bạn hãy cập nhật `appsettings.json` và tạo file `VangTodayPriceResponse.cs`. Sau khi xong, hãy báo cho mình để chúng ta cùng sang **Bước V3.3 & V3.4: Xây dựng Interface và Typed HttpClient Provider** nhé!