Được. Nếu V2 + V3 đã chạy ổn thì **tôi khuyên chúng ta đi thẳng V4, nhưng chỉ làm V4 trước**, chưa nhảy một lúc tới V5/V6.

Lý do: từ V4 trở đi GoldWatch bắt đầu chuyển từ CRUD API sang **backend có data pipeline thực sự**. V4 là nền móng cho toàn bộ phần AI sau này.

### V4 — News Collector

Mục tiêu:

```text
News Source
    ↓
HTTP / RSS / Scraper
    ↓
Raw Article
    ↓
Normalize
    ↓
Deduplicate
    ↓
PostgreSQL
```

Ta sẽ thêm 3 thành phần chính:

```text
Models/
├── NewsSource.cs
├── NewsArticle.cs
└── NewsAnalysis.cs       // chưa dùng ở V4

Services/
├── News/
│   ├── INewsCollector.cs
│   ├── NewsCollector.cs
│   └── ...
```

Database:

```text
news_sources
news_articles
news_analyses   ← chuẩn bị cho V6
gold_prices     ← V2/V3
```

### V4.1 — NewsSource

Trước tiên tạo được nguồn tin:

```text
GET    /api/news/sources
POST   /api/news/sources
PUT    /api/news/sources/{id}
DELETE /api/news/sources/{id}
```

Ví dụ:

```json
{
  "name": "Example News",
  "baseUrl": "https://example.com",
  "sourceType": "Private",
  "isActive": true
}
```

Ta sẽ dùng `enum` cho:

```csharp
Government
Private
International
```

---

### V4.2 — NewsArticle

Entity:

```text
NewsArticle
├── Id
├── SourceId
├── Title
├── Url
├── Content
├── Author
├── PublishedAt
└── CollectedAt
```

Quan hệ:

```text
NewsSource
    │
    └── 1 : N
          │
          ▼
     NewsArticle
```

Đây là lúc bạn sẽ học một phần EF Core quan trọng hơn V2:

* Foreign Key
* Navigation Property
* `Include`
* relationship configuration
* unique constraint/index
* migration thay đổi schema

---

### V4.3 — Collector

Đây mới là phần quan trọng nhất.

Không để controller làm chuyện này:

```text
Controller
    ↓
HttpClient
    ↓
Parse HTML
    ↓
Save DB
```

Mà:

```text
Controller
    ↓
NewsCollector
    ↓
NewsSourceAdapter
    ↓
HttpClient
    ↓
Parser
    ↓
NewsArticle
    ↓
EF Core
```

Và tôi muốn chúng ta **không bắt đầu bằng web scraping HTML phức tạp**.

Ưu tiên:

```text
RSS/Atom
   ↓
Parse
   ↓
NewsArticle
```

Sau đó mới xử lý website không có RSS.

---

### V4.4 — Deduplication

Đây là một bài tập backend rất đáng giá.

Ví dụ collector chạy 3 lần:

```text
https://news.com/gold/fed-rate-cut
https://news.com/gold/fed-rate-cut
https://news.com/gold/fed-rate-cut
```

Database không được thành:

```text
Article #1
Article #2
Article #3
```

Ta sẽ thiết kế:

```text
Url
 ↓
Normalize
 ↓
Unique constraint
 ↓
Skip duplicate
```

Có thể sau này bổ sung hash:

```text
SHA256(title + content)
```

để phát hiện những bài có URL khác nhau nhưng nội dung gần như giống nhau.

---

### V4.5 — API

Cuối V4 tôi muốn GoldWatch có những API kiểu:

```http
GET /api/news
GET /api/news/{id}

GET /api/news/sources

POST /api/news/collect
```

Ví dụ:

```http
POST /api/news/collect
```

sẽ:

```text
Active Sources
      ↓
Collect
      ↓
Parse
      ↓
Normalize
      ↓
Deduplicate
      ↓
Save PostgreSQL
      ↓
Return result
```

Response có thể kiểu:

```json
{
  "sourcesProcessed": 3,
  "articlesFound": 47,
  "articlesAdded": 31,
  "duplicatesSkipped": 16
}
```

---

## Sau V4 thì roadmap trở nên rất thú vị

Tôi đề xuất điều chỉnh roadmap thành:

```text
V0  Specification
 │
V1  ASP.NET Core Web API
 │
V2  PostgreSQL + EF Core
 │
V3  External Gold Price API
 │
▼
V4  News Collection              ← NOW
 │
V5  Background Worker
 │
V6  LLM Article Analysis
 │
V7  Gold Trend Analysis
 │
V8  Angular Dashboard
 │
V9  Authentication / Security
 │
V10 Testing + Docker + CI/CD
```

Điểm quan trọng là **V5 sẽ biến V4 từ một API thủ công thành một hệ thống tự chạy**:

```text
               ┌── Gold Price API
               │
Background ────┼── News Sources
Worker         │
               └── ...
                    ↓
                PostgreSQL
                    ↓
                   AI
```

Và V6 lúc đó mới thật sự đáng học:

```text
NewsArticle
     ↓
LLM
     ↓
Sentiment
Summary
Key Factors
Relevance
     ↓
NewsAnalysis
```

Sau đó V7 mới kết hợp:

```text
Gold Price History
        +
NewsAnalysis
        ↓
   Trend Analysis
        ↓
┌─────────────────────┐
│ Price trend         │
│ News sentiment      │
│ Key factors         │
│ Confidence          │
│ Explanation         │
└─────────────────────┘
```

Đây cũng là chỗ chiếc **RTX PRO 4000 24GB** của bạn sẽ bắt đầu có đất dụng võ nếu sau này chúng ta muốn thay LLM API bằng model local.

**Vậy tôi đề xuất bắt đầu V4.1: thiết kế `NewsSource` + EF Core relationship + migration trước.** Sau khi phần này chạy ổn, ta mới sang collector/RSS thay vì ném cả V4 vào một lần.
