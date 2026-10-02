Được. Với trạng thái hiện tại của bạn, mình nghĩ project này **rất phù hợp để trở thành project ASP.NET “thực chiến” song song với SchoolCRUDConsole**.

Điểm quan trọng là **không nhảy ngay vào AI hay Angular**. Ta sẽ dùng project này để chuyển từ tư duy:

> `Console CRUD → ASP.NET Core Web API → Database → External API/Crawler → Background Jobs → AI/LLM → Angular`

và mỗi version sẽ tương ứng với một nhóm kiến thức phỏng vấn .NET/backend.

Mình đề xuất đặt tên tạm là **GoldWatch**.

---

# 1. Project tổng thể: GoldWatch

Mục tiêu cuối cùng:

> Một web application theo dõi giá vàng, thu thập tin tức về vàng từ nhiều nguồn, lưu trữ dữ liệu, phân tích nội dung bằng LLM và đưa ra xu hướng/thống kê để người dùng theo dõi.

Kiến trúc cuối project có thể đi theo hướng:

```text
                    ┌──────────────────┐
                    │     Angular      │
                    │   Web Frontend   │
                    └────────┬─────────┘
                             │ HTTP
                             ▼
┌───────────────────────────────────────────────────┐
│                  ASP.NET Core API                 │
│                                                   │
│  Gold Price │ News │ Analysis │ Dashboard        │
└───────────┬──────────────┬──────────────┬─────────┘
            │              │              │
            ▼              ▼              ▼
       PostgreSQL      Background Job    AI Service
            │              │              │
            │              ▼              ▼
            │         News Crawler      LLM API
            │
            ▼
      Gold Price Data
```

Về sau có thể mở rộng:

```text
                    GoldWatch
                       │
        ┌──────────────┼──────────────┐
        ▼              ▼              ▼
   Price Tracking   News Analysis   AI Insight
        │              │              │
        ▼              ▼              ▼
 Historical data    Government      Sentiment
                   Private news     Trend
                                    Summary
```

---

# 2. Một điểm rất quan trọng: đừng làm "AI dự đoán giá vàng"

Ở phiên bản đầu, mình **không khuyên** đặt mục tiêu:

> "AI dự đoán ngày mai vàng tăng hay giảm."

Thay vào đó:

> **AI phân tích các bài báo và dữ liệu giá để mô tả xu hướng hiện tại.**

Ví dụ:

```text
Price:
Gold increased +1.8% over 7 days

News:
12 articles collected

LLM analysis:
- USD weakening mentioned frequently
- Global interest-rate expectations mentioned frequently
- Domestic demand mentioned moderately

Overall sentiment:
Bullish

Confidence:
0.72
```

Như vậy project vừa có AI nhưng vẫn giữ được tính kỹ thuật/backend rõ ràng.

Và quan trọng hơn: **đừng để LLM tự quyết định dữ liệu định lượng**.

Ví dụ:

```text
Price calculation
       │
       ▼
ASP.NET / Database
       │
       ├── +3.2%
       ├── 7-day average
       └── volume
       
LLM
       │
       ▼
Interpretation
```

LLM giải thích dữ liệu, còn calculation nên do code/database đảm nhiệm.

---

# 3. Roadmap tổng thể

Mình đề xuất khoảng **10 versions**.

| Version | Nội dung chính                       | Bạn học được                         |
| ------- | ------------------------------------ | ------------------------------------ |
| V0      | Project specification                | System design                        |
| V1      | ASP.NET Core Web API                 | ASP.NET fundamentals                 |
| V2      | Database + EF Core                   | ORM, SQL, migrations                 |
| V3      | Gold price ingestion                 | External API, HttpClient             |
| V4      | News ingestion                       | Crawler, parsing                     |
| V5      | Background processing                | Worker Service, async                |
| V6      | News analysis                        | LLM integration                      |
| V7      | AI pipeline                          | AI architecture                      |
| V8      | Angular frontend                     | Frontend/API integration             |
| V9      | Authentication + production concerns | JWT, security                        |
| V10     | Production architecture              | Docker, caching, testing, deployment |

Và **mỗi version sẽ chạy được**.

Đây là điểm mình muốn giữ giống SchoolCRUDConsole.

---

# V0 — Specification & Architecture

Đầu tiên **chưa code**.

Ta xác định domain.

### Entities ban đầu

```text
GoldPrice
NewsArticle
NewsSource
NewsAnalysis
```

Có thể hình dung:

```text
GoldPrice

Id
Source
GoldType
BuyPrice
SellPrice
Currency
CollectedAt
```

---

```text
NewsSource

Id
Name
Url
Type
IsActive
```

Ví dụ:

```text
Government
Private
International
```

---

```text
NewsArticle

Id
Title
Url
Content
PublishedAt
CollectedAt
SourceId
```

---

```text
NewsAnalysis

Id
ArticleId
Sentiment
Summary
KeyFactors
Model
AnalyzedAt
```

---

## API dự kiến

Sau này:

```http
GET /api/gold/prices
GET /api/gold/prices/latest
GET /api/gold/prices/history

GET /api/news
GET /api/news/{id}

GET /api/news/sources

GET /api/analysis/latest
GET /api/analysis/{articleId}
```

Nhưng V1 **chưa cần tất cả**.

---

# V1 — ASP.NET Core Web API

Đây sẽ là bước chuyển quan trọng nhất từ SchoolCRUDConsole.

Project:

```text
GoldWatch.Api
```

Cấu trúc ban đầu:

```text
GoldWatch
│
├── GoldWatch.Api
│   ├── Controllers
│   ├── Models
│   ├── Services
│   ├── DTOs
│   └── Program.cs
│
└── GoldWatch.sln
```

Ban đầu **chưa database**.

Ta hard-code:

```csharp
List<GoldPrice>
```

và xây:

```http
GET /api/gold
POST /api/gold
GET /api/gold/{id}
PUT /api/gold/{id}
DELETE /api/gold/{id}
```

Mục tiêu V1:

```text
Browser / Swagger
       │
       ▼
Controller
       │
       ▼
Service
       │
       ▼
In-memory List
```

Bạn sẽ học:

* ASP.NET Core
* Controller
* Routing
* HTTP
* REST API
* DTO
* Dependency Injection
* Service layer
* Swagger
* HTTP status codes
* `IActionResult`
* `async Task`
* configuration cơ bản

Đây chính là lúc kiến thức từ SchoolCRUDConsole bắt đầu chuyển sang web.

---

# V2 — PostgreSQL + Entity Framework Core

Sau khi API chạy ổn:

```text
Controller
    ↓
Service
    ↓
Repository / DbContext
    ↓
PostgreSQL
```

Ta thêm:

```text
GoldWatch.Infrastructure
```

hoặc ban đầu giữ đơn giản:

```text
GoldWatch.Api
GoldWatch.Domain
GoldWatch.Infrastructure
```

Database:

```text
gold_prices
news_sources
news_articles
news_analyses
```

Học:

### EF Core

```csharp
DbContext
DbSet<T>
```

### Migration

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### LINQ

```csharp
context.GoldPrices
    .Where(...)
    .OrderByDescending(...)
    .Take(20)
```

### Quan trọng hơn

Bạn sẽ bắt đầu hiểu:

```text
Model
Entity
DTO
Database Model
```

không phải tất cả đều là một class.

---

# V3 — Gold Price Collector

Đây là version rất thú vị.

Thay vì:

```text
POST /api/gold
```

nhập giá thủ công, hệ thống sẽ lấy dữ liệu từ một nguồn bên ngoài.

```text
External Gold Price Source
          │
          │ HTTP
          ▼
ASP.NET Core
          │
          ▼
GoldPriceService
          │
          ▼
PostgreSQL
```

Học:

```csharp
HttpClient
IHttpClientFactory
JSON serialization
System.Text.Json
API integration
```

Ví dụ service:

```csharp
public async Task<GoldPrice> FetchGoldPriceAsync()
{
    ...
}
```

Sau đó:

```http
GET /api/gold/latest
```

có thể trả:

```json
{
    "buyPrice": 14230000,
    "sellPrice": 14450000,
    "collectedAt": "..."
}
```

---

# V4 — News Crawler

Đây sẽ là một bước nâng cấp khá lớn.

Ta có:

```text
News Sources
     │
     ├── Government
     ├── Private
     └── International
```

Collector:

```text
NewsCrawler
    │
    ├── Source A
    ├── Source B
    └── Source C
```

Mục tiêu:

```text
URL
 ↓
HTTP GET
 ↓
HTML
 ↓
Parser
 ↓
NewsArticle
 ↓
Database
```

Bạn sẽ học:

* HTML parsing
* DOM
* CSS selector
* HTTP headers
* timeout
* retry
* duplicate detection
* URL normalization
* crawler design

Đặc biệt, ta sẽ thiết kế:

```csharp
INewsCrawler
```

rồi:

```csharp
GovernmentNewsCrawler
PrivateNewsCrawler
```

Đây là chỗ **OOP + design pattern từ SchoolCRUDConsole V5/V6** bắt đầu phát huy rõ ràng.

---

# V5 — Background Processing

Đây là version mình rất muốn bạn làm.

Không muốn user phải gọi:

```http
POST /api/crawl
```

mỗi lần.

Thay vào đó:

```text
                 ┌───────────────┐
                 │ Background Job │
                 └───────┬───────┘
                         │
             ┌───────────┴───────────┐
             ▼                       ▼
       Gold Collector          News Crawler
             │                       │
             └───────────┬───────────┘
                         ▼
                     Database
```

Có thể bắt đầu bằng:

```csharp
BackgroundService
```

hoặc:

```csharp
IHostedService
```

Ví dụ:

```text
Every 30 minutes:
    Fetch gold price

Every 1 hour:
    Crawl news
```

Bạn sẽ học:

* Worker Service
* BackgroundService
* cancellation token
* graceful shutdown
* async processing
* dependency injection trong background worker
* scheduling

Đây là kiến thức backend interview rất đáng giá.

---

# V6 — LLM Integration

**Đây mới là lúc AI xuất hiện.**

Pipeline:

```text
NewsArticle
     │
     ▼
Prepare Prompt
     │
     ▼
LLM
     │
     ▼
Structured Response
     │
     ▼
NewsAnalysis
     │
     ▼
Database
```

Ví dụ LLM nhận:

```text
Title:
"Fed signals..."

Content:
"..."
```

và trả:

```json
{
    "sentiment": "Bullish",
    "summary": "...",
    "factors": [
        "Interest rate expectations",
        "USD movement"
    ]
}
```

Điểm quan trọng:

**không lưu raw LLM response rồi tự parse lung tung.**

Ta muốn structured output.

```csharp
public class NewsAnalysisResult
{
    public string Sentiment { get; set; }
    public string Summary { get; set; }
    public List<string> Factors { get; set; }
}
```

---

# V7 — AI Analysis Pipeline

V6 chỉ là:

```text
Article → LLM
```

V7 sẽ thành:

```text
                    ┌── Article
                    │
                    ├── Gold price
                    │
                    ├── Recent articles
                    │
                    ▼
              Analysis Pipeline
                    │
                    ▼
                   LLM
                    │
                    ▼
              Market Insight
```

Ví dụ:

```json
{
    "period": "7d",
    "priceChange": 2.8,
    "sentiment": "Bullish",
    "majorFactors": [
        "USD weakness",
        "Interest rate expectations",
        "Geopolitical uncertainty"
    ],
    "summary": "..."
}
```

Lúc này AI không còn đơn giản là:

> "Summarize article."

mà trở thành:

> "Analyze a collection of information."

Đây chính là bước đầu của **AI application architecture**.

---

# V8 — Angular Frontend

Chỉ tới đây chúng ta mới đầu tư nghiêm túc vào Angular.

Frontend đầu tiên cực kỳ đơn giản:

```text
┌─────────────────────────────────────────┐
│              GOLD WATCH                 │
├─────────────────────────────────────────┤
│                                         │
│ Gold Price                              │
│                                         │
│ Buy       Sell       Change             │
│ 1423      1445       +1.8%              │
│                                         │
├─────────────────────────────────────────┤
│ Price Chart                             │
│                                         │
│       ╭────╮                            │
│   ╭───╯    ╰────╮                       │
│ ──╯              ╰──                    │
│                                         │
├─────────────────────────────────────────┤
│ Latest News                             │
│                                         │
│ [Article]                               │
│ [Article]                               │
│ [Article]                               │
└─────────────────────────────────────────┘
```

Angular:

```text
DashboardComponent
GoldPriceComponent
PriceChartComponent
NewsListComponent
NewsDetailComponent
AnalysisComponent
```

API:

```text
Angular
   │
   │ HTTP
   ▼
ASP.NET Core
```

Bạn sẽ học:

* Angular components
* services
* HttpClient
* routing
* RxJS
* Observables
* interfaces/models
* API integration

---

# V9 — Authentication & Security

Khi application đã có giá trị thực:

```text
Angular
   │
   │ JWT
   ▼
ASP.NET API
```

Thêm:

```text
User
Role
Authentication
Authorization
```

Ví dụ:

```text
Anonymous
    │
    ├── xem giá
    └── xem news

Admin
    │
    ├── manage sources
    ├── trigger crawler
    └── inspect AI jobs
```

Học:

* JWT
* authentication vs authorization
* claims
* role
* password hashing
* CORS
* secrets/configuration

---

# V10 — Production Version

Đây là version biến project từ:

> "student project"

thành:

> "portfolio/backend project".

Thêm:

### Logging

```text
Serilog
```

### Global exception handling

```text
Exception Middleware
```

### Validation

```text
FluentValidation
```

### Caching

```text
Redis
```

### Docker

```text
Angular
ASP.NET
PostgreSQL
Redis
```

### Testing

```text
Unit Test
Integration Test
```

### CI/CD

Có thể:

```text
GitHub
   ↓
GitHub Actions
   ↓
Build
   ↓
Test
   ↓
Docker
```

---

# 4. Sau toàn bộ project, architecture sẽ khá đẹp

Ví dụ:

```text
GoldWatch
│
├── src
│   │
│   ├── GoldWatch.Api
│   │
│   ├── GoldWatch.Application
│   │   ├── Gold
│   │   ├── News
│   │   └── Analysis
│   │
│   ├── GoldWatch.Domain
│   │   ├── Entities
│   │   ├── Enums
│   │   └── Interfaces
│   │
│   ├── GoldWatch.Infrastructure
│   │   ├── Persistence
│   │   ├── Crawlers
│   │   ├── ExternalApis
│   │   └── AI
│   │
│   └── GoldWatch.Worker
│
├── tests
│   ├── GoldWatch.UnitTests
│   └── GoldWatch.IntegrationTests
│
└── frontend
    └── goldwatch-angular
```

Nhưng **đừng tạo structure này ngay từ V1**.

Đây là một nguyên tắc mình muốn áp dụng cho project này:

> **Architecture should evolve with requirements.**

Nếu ngay V1 chúng ta tạo Clean Architecture + Repository + CQRS + MediatR + Redis + Kafka thì bạn sẽ học framework thay vì học backend.

---

# 5. Mối liên hệ với SchoolCRUDConsole

Đây mới là phần mình nghĩ sẽ rất có lợi cho bạn.

Hai project sẽ bổ trợ nhau:

```text
             SchoolCRUDConsole
                    │
             OOP / Patterns
                    │
                    ▼
              GoldWatch
                    │
       ┌────────────┼────────────┐
       ▼            ▼            ▼
    ASP.NET       Database       AI
       │            │            │
       └────────────┼────────────┘
                    ▼
                 Angular
```

Ví dụ:

| SchoolCRUD           | GoldWatch                    |
| -------------------- | ---------------------------- |
| Entity               | Entity                       |
| Interface            | Service Interface            |
| Repository           | EF Core Repository/DbContext |
| Service              | Application Service          |
| Strategy             | Crawler Strategy             |
| Factory              | Crawler Factory              |
| CRUD                 | REST API                     |
| File logging         | Structured logging           |
| Random simulation    | Background jobs              |
| JSON                 | API JSON                     |
| `async Task`         | HTTP/DB/LLM async            |
| Dependency Injection | ASP.NET DI                   |

Nói cách khác:

**SchoolCRUD là laboratory để học C#/OOP.**

**GoldWatch là laboratory để đưa những thứ đó vào backend thực tế.**

---

# 6. Cách chúng ta sẽ học từng version

Mình đề xuất mỗi version đều theo format cố định:

### ① Requirement

Ví dụ:

```text
GoldWatch V1 Requirements
```

### ② Concepts

Ta xác định trước:

```text
REST
Controller
DTO
DI
Service
HTTP
```

### ③ Architecture

```text
Client
  ↓
Controller
  ↓
Service
  ↓
Repository
```

### ④ Implementation

Ta **code từng bước**, không đưa toàn bộ project một lần.

### ⑤ Test

Ví dụ:

```http
GET /api/gold
POST /api/gold
GET /api/gold/1
```

### ⑥ Refactoring

Xem:

```text
Có coupling không?
Có violation SOLID không?
Có duplicate code không?
```

### ⑦ Interview knowledge

Cuối version sẽ có:

> **"Nếu interviewer hỏi về những thứ vừa làm thì bạn phải giải thích được gì?"**

---

# 7. Thứ tự mình đặc biệt khuyên bạn giữ

Đừng làm:

```text
ASP.NET
 ↓
Angular
 ↓
AI
 ↓
Database
```

Mà:

```text
                ┌─────────────┐
                │ ASP.NET API │
                └──────┬──────┘
                       │
                 PostgreSQL
                       │
                       ▼
                 External APIs
                       │
                       ▼
                 Background Jobs
                       │
                       ▼
                     LLM
                       │
                       ▼
                  AI Analysis
                       │
                       ▼
                    Angular
```

Tức là:

**Backend → Data → Integration → Background processing → AI → Frontend.**

Đúng với mục tiêu hiện tại của bạn.

---

# 8. Và project này có một "difficulty curve" khá đẹp

```text
Difficulty
   │
   │                                    V10
   │                              ┌───────
   │                         V9 ──┘
   │                    V8 ──┘
   │               V7 ──┘
   │          V6 ──┘
   │      V5 ─┘
   │   V4 ─┘
   │ V3
   │V2
   │V1
   └──────────────────────────────────────
```

Đặc biệt:

### V1–V3

> **ASP.NET developer**

### V4–V5

> **Backend engineer**

### V6–V7

> **AI application developer**

### V8

> **Full-stack developer**

### V9–V10

> **Production-oriented developer**

Đây cũng khá khớp với mục tiêu .NET backend của bạn: **AI chỉ là một capability của backend, không biến project thành một project prompt engineering thuần túy.**

---

## Bước tiếp theo: V0 — Specification

Mình đề nghị **chưa tạo project ngay**.

Ta sẽ bắt đầu bằng một **Software Specification Requirements cho GoldWatch V1–V10**, tương tự cách chúng ta đã làm với SchoolCRUDConsole. Trong đó mình sẽ chốt:

* Functional Requirements
* Non-functional Requirements
* Entity/domain model
* API roadmap
* Database roadmap
* Crawler requirements
* AI requirements
* Angular requirements
* Version boundaries
* Definition of Done cho từng version
* Kiến thức .NET/backend cần học ở mỗi version
* Những thứ **cố tình chưa làm** để tránh scope creep

Sau khi V0 chốt xong, chúng ta bắt đầu **V1 bằng việc tạo ASP.NET Core Web API đầu tiên**, và mình sẽ đi với bạn từng bước, thay vì dump toàn bộ code một lần.
