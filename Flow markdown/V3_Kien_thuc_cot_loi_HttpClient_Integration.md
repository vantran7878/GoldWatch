# Tổng Hợp Kiến Thức Cốt Lõi Version 3: External API, Typed HttpClient & Data Ingestion

Tài liệu này tổng hợp toàn bộ lý thuyết kiến trúc, nguyên lý tích hợp hệ thống bên ngoài và các kỹ thuật C# / .NET nâng cao bạn đã làm chủ trong **Version 3**.

---

## MỤC LỤC
1. [Bức Tranh Toàn Cảnh: Chuyển Mình Sang Ingestion System](#1-bức-tranh-toàn-cảnh-chuyển-mình-sang-ingestion-system)
2. [Tầng Chống Tha Hóa (Anti-Corruption Layer) & Tách Biệt External DTO](#2-tầng-chống-tha-hóa-anti-corruption-layer--tách-biệt-external-dto)
3. [HttpClient Trong .NET: Thảm Họa Socket Exhaustion & Typed Client Pattern](#3-httpclient-trong-net-thảm-họa-socket-exhaustion--typed-client-pattern)
4. [Cơ Chế Hủy Tác Vụ Thông Minh (`CancellationToken`)](#4-cơ-chế-hủy-tác-vụ-thông-minh-cancellationtoken)
5. [Cú Pháp & Kỹ Thuật Xử Lý Dữ Liệu Nổi Bật Trong V3](#5-cú-pháp--kỹ-thuật-xử-lý-dữ-liệu-nổi-bật-trong-v3)
6. [Tại Sao Endpoint Sync Dùng HTTP `POST` Mà Không Dùng `GET`?](#6-tại-sao-endpoint-sync-dùng-http-post-mà-không-dùng-get)
7. [Bảng Câu Hỏi Phỏng Vấn Trọng Tâm V3](#7-bảng-câu-hỏi-phỏng-vấn-trọng-tâm-v3)

---

## 1. Bức Tranh Toàn Cảnh: Chuyển Mình Sang Ingestion System

Ở V1 và V2, hệ thống chỉ là một ứng dụng CRUD bị động: Dữ liệu do con người gõ bằng tay đưa vào.  
Ở V3, GoldWatch chính thức trở thành **Hệ thống thu thập dữ liệu (Ingestion System)**: Tự động kết nối ra thế giới bên ngoài (Internet), lấy dữ liệu thời gian thực và nạp vào cơ sở dữ liệu nội bộ.

```text
                               LUỒNG DỮ LIỆU VERSION 3
                               
 ┌──────────────────┐
 │ Trình duyệt / UI │
 └────────┬─────────┘
          │ 1. POST /api/gold/sync (kèm CancellationToken)
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ GoldController                                         │
 └────────┬───────────────────────────────────────────────┘
          │ 2. Gọi Service
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ GoldPriceService                                       │
 └────────┬───────────────────────────────────────────────┘
          │ 3. Gọi Adapter
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ IGoldPriceProvider (Typed HttpClient)                  │
 └────────┬───────────────────────────────────────────────┘
          │ 4. HTTP GET (ra ngoài Internet)
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ External Server: https://www.vang.today/api/prices     │
 └────────┬───────────────────────────────────────────────┘
          │ 5. Trả về JSON: {"type": "SJL1L10", "buy": 85500000, ...}
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ VangTodayPriceResponse (External DTO)                  │
 └────────┬───────────────────────────────────────────────┘
          │ 6. Map sang Domain Model GoldPrice
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ GoldPrice Entity  (BuyPrice = 85500000, Currency = VND)│
 └────────┬───────────────────────────────────────────────┘
          │ 7. INSERT INTO "GoldPrices" ... RETURNING "Id"
          ▼
 ┌────────────────────────────────────────────────────────┐
 │ PostgreSQL Database                                    │
 └────────────────────────────────────────────────────────┘
```

---

## 2. Tầng Chống Tha Hóa (Anti-Corruption Layer) & Tách Biệt External DTO

### 2.1. Vấn đề "Dính chặt" (Tight Coupling)
Nhiều lập trình viên mới thường có thói quen: Dùng luôn class `GoldPrice` (Domain Model) để deserialize dữ liệu nhận về từ API ngoài:
```csharp
// PHƯƠNG PHÁP TỒI (Anti-pattern):
var price = await _httpClient.GetFromJsonAsync<GoldPrice>("prices?type=SJL1L10");
```
**Hậu quả khôn lường:**
1. **Bị phụ thuộc vào bên thứ 3:** Nếu `vang.today` đổi tên trường JSON từ `"buy"` thành `"buy_price"`, model của bạn bị hỏng, kéo theo database của bạn cũng phải đổi tên cột theo họ!
2. **Lộ cấu trúc nội bộ:** Nếu API ngoài thêm bớt trường rác, Domain Model của bạn bị "tha hóa" (corrupted) bởi những trường không cần thiết.

### 2.2. Giải pháp: Mẫu thiết kế Anti-Corruption Layer (ACL)
Chúng ta tách thành 2 thế giới riêng biệt:

```text
   BÊN NGOÀI (External Contract)          BÊN TRONG (Internal Domain)
 ┌───────────────────────────────┐     ┌───────────────────────────────┐
 │   VangTodayPriceResponse      │     │          GoldPrice            │
 ├───────────────────────────────┤     ├───────────────────────────────┤
 │ string Type                   │     │ int Id                        │
 │ string Name                   │ ──> │ string GoldType               │
 │ decimal Buy                   │ ──> │ decimal BuyPrice              │
 │ decimal Sell                  │ ──> │ decimal SellPrice             │
 │ long Timestamp                │ ──> │ DateTime CollectedAt          │
 └───────────────────────────────┘     │ string Currency = "VND"       │
                                       │ string Source = "vang.today"  │
                                       └───────────────────────────────┘
```

* `VangTodayPriceResponse` khớp 100% với JSON của đối tác.
* `GoldPriceProvider` đóng vai trò là chiếc cầu dịch chuyển (Adapter), bảo vệ Domain Model `GoldPrice` luôn sạch sẽ.

---

## 3. HttpClient Trong .NET: Thảm Họa Socket Exhaustion & Typed Client Pattern

### 3.1. Thảm họa Cạn kiệt Socket (`new HttpClient` Anti-pattern)

Nhiều người nghĩ `HttpClient` kế thừa `IDisposable` nên viết như sau:
```csharp
// TUYỆT ĐỐI KHÔNG DÙNG:
using (var client = new HttpClient())
{
    var data = await client.GetStringAsync("...");
}
```
**Tại sao sai?**
* Khi gọi `Dispose()`, kết nối TCP socket ngầm bên dưới của hệ điều hành **không đóng ngay lập tức**. Nó rơi vào trạng thái chờ `TIME_WAIT` trong khoảng **240 giây** (4 phút).
* Nếu ứng dụng có 1.000 request/phút, toàn bộ socket của hệ điều hành sẽ bị chiếm dụng hết $\rightarrow$ Gây lỗi **`SocketException: Only one usage of each socket address is normally permitted` (Socket Exhaustion)** làm sập cả server.

### 3.2. Vấn đề DNS Stale (Khi dùng `static HttpClient`)
Nếu bạn khắc phục bằng cách tạo `private static readonly HttpClient _client = new HttpClient();`:
* Socket không bị cạn kiệt, nhưng nảy sinh lỗi **DNS Stale**: Client giữ mãi kết nối TCP cũ. Nếu máy chủ bên kia thay đổi địa chỉ IP (đổi server, scale up), ứng dụng của bạn sẽ không nhận biết được và gọi vào địa chỉ IP chết.

### 3.3. Giải pháp chuẩn mực của .NET: `IHttpClientFactory` & Typed Client Pattern
Trong `Program.cs`, chúng ta đăng ký:
```csharp
builder.Services.AddHttpClient<IGoldPriceProvider, VangTodayGoldPriceProvider>(client =>
{
    var baseUrl = builder.Configuration["GoldPriceApi:BaseUrl"] ?? "https://www.vang.today/api/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

**Cơ chế hoạt động kỳ diệu của `IHttpClientFactory`:**
1. Nó quản lý một **Connection Pool** chứa các `HttpMessageHandler`.
2. Các kết nối TCP được **tái sử dụng liên tục** $\rightarrow$ Không bao giờ bị Socket Exhaustion.
3. Mỗi `HttpMessageHandler` có vòng đời mặc định là **2 phút**. Hết 2 phút, nó tự tạo handler mới để cập nhật lại bản ghi DNS mới nhất $\rightarrow$ Giải quyết triệt để lỗi DNS Stale.
4. **Typed Client**: `VangTodayGoldPriceProvider` chỉ cần khai báo `public VangTodayGoldPriceProvider(HttpClient httpClient)` trong constructor, ASP.NET Core sẽ tự động bơm đúng instance `HttpClient` đã cấu hình sẵn `BaseAddress` và `Timeout`.

---

## 4. Cơ Chế Hủy Tác Vụ Thông Minh (`CancellationToken`)

Khi gọi API bên ngoài, độ trễ mạng có thể kéo dài vài giây.  
Nếu người dùng nhấn nút Sync trên web nhưng sau đó sốt ruột **tắt tab hoặc bấm Refresh liên tục**:

```text
[Người dùng tắt Tab]
        │
        ▼
ASP.NET Core phát hiện kết nối HTTP Client bị đứt
        │
        ▼
Kích hoạt: HttpContext.RequestAborted (CancellationToken chuyển sang trạng thái IsCancellationRequested = true)
        │
        ▼
Lan truyền vào: GoldController -> GoldPriceService -> HttpClient.GetFromJsonAsync(..., cancellationToken)
        │
        ▼
Lập tức NGẮT CUỘC GỌI HTTP ra ngoài Internet, giải phóng Thread & RAM ngay tại thời điểm đó!
```

> **Bài học vàng:** Luôn truyền `CancellationToken` xuống tận đáy của các hàm I/O mạng và Database (`SaveChangesAsync(cancellationToken)`).

---

## 5. Cú Pháp & Kỹ Thuật Xử Lý Dữ Liệu Nổi Bật Trong V3

### 5.1. Thuộc tính `[JsonPropertyName]`
Dùng để ánh xạ tên thuộc tính C# và khóa trong JSON:
```csharp
[JsonPropertyName("buy")]
public decimal Buy { get; set; }
```

### 5.2. `GetFromJsonAsync<T>`
Thuộc thư viện `System.Net.Http.Json`, thực hiện gửi GET + đọc stream + parse JSON sang C# object chỉ với 1 dòng:
```csharp
var response = await _httpClient.GetFromJsonAsync<VangTodayPriceResponse>(endpoint, cancellationToken);
```

### 5.3. Chuyển đổi Unix Epoch Timestamp sang `DateTime` UTC
Dữ liệu ngày giờ từ API quốc tế thường trả về số nguyên Unix timestamp (số giây tính từ 01/01/1970). Cách chuyển đổi chuẩn trong C#:
```csharp
DateTime collectedAt = DateTimeOffset.FromUnixTimeSeconds(response.Timestamp).UtcDateTime;
```

---

## 6. Tại Sao Endpoint Sync Dùng HTTP `POST` Mà Không Dùng `GET`?

| Tiêu chí | `GET /api/gold/sync` ❌ | `POST /api/gold/sync` ✅ |
| :--- | :--- | :--- |
| **Tính an toàn (Safety)** | **Vi phạm:** Chuẩn HTTP quy định `GET` là Safe Method (chỉ đọc, không được sửa đổi dữ liệu máy chủ). | **Đúng chuẩn:** `POST` là Unsafe Method, dành riêng cho các thao tác làm thay đổi dữ liệu (gọi INSERT DB). |
| **Bộ nhớ đệm (Caching)** | Trình duyệt / CDN có thể cache lại kết quả `GET`, khiến lần gọi sau không thực sự gọi API ngoài. | `POST` không bao giờ bị tự động cache. |
| **Web Crawlers (Bot tìm kiếm)** | Các con bot (Google, Bing) quét link `GET` sẽ vô tình trigger hàng trăm lệnh sync làm rác database. | Bot tìm kiếm không bao giờ tự ý gửi request `POST`. |
| **Tính lũy đọng (Idempotency)** | Sai ngữ nghĩa (mỗi lần gọi lại sinh ra 1 bản ghi mới với ID mới). | Chuẩn ngữ nghĩa (mỗi lần gọi sinh ra một tài nguyên mới). |

---

## 7. Bảng Câu Hỏi Phỏng Vấn Trọng Tâm V3

1. **Tại sao không nên tạo trực tiếp `new HttpClient()` trong Controller hay Service?**
   * *Trả lời:* Vì gây ra lỗi **Socket Exhaustion** (cạn kiệt cổng mạng do các kết nối TCP rơi vào trạng thái `TIME_WAIT` kéo dài đến 4 phút trước khi được giải phóng).
2. **`IHttpClientFactory` giải quyết bài toán DNS Stale như thế nào?**
   * *Trả lời:* Nó xoay vòng và làm mới các `HttpMessageHandler` ngầm định kỳ 2 phút một lần, giúp cập nhật địa chỉ IP mới nhất từ máy chủ DNS trong khi vẫn tái sử dụng được connection pool.
3. **Mục đích của việc tạo DTO riêng (`VangTodayPriceResponse`) thay vì dùng thẳng Entity `GoldPrice` là gì?**
   * *Trả lời:* Áp dụng mô hình **Anti-Corruption Layer (ACL)** để tách biệt hợp đồng dữ liệu của bên thứ ba với mô hình nghiệp vụ nội bộ, tránh việc hệ thống bị vỡ khi đối tác thay đổi cấu trúc API.
4. **`CancellationToken` hoạt động như thế nào khi một HTTP Request bị người dùng hủy bỏ?**
   * *Trả lời:* ASP.NET Core kích hoạt tín hiệu hủy qua `HttpContext.RequestAborted`. Tín hiệu này lan truyền xuống `HttpClient` và Database để dừng ngay lập tức việc truyền dữ liệu qua socket, ném ra ngoại lệ `OperationCanceledException` và giải phóng tài nguyên CPU/RAM.
