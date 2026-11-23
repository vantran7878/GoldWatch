# 📈 GoldWatch — Hệ Thống Theo Dõi & Phân Tích Giá Vàng (Fullstack Application)

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular" />
  <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/Chart.js-FF6384?style=for-the-badge&logo=chartdotjs&logoColor=white" alt="Chart.js" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License" />
</p>

---

## 📖 Giới Thiệu (About GoldWatch)

**GoldWatch** là một ứng dụng web Fullstack hiện đại phục vụ việc theo dõi, đồng bộ và phân tích biến động giá vàng thời gian thực và lịch sử tại thị trường Việt Nam. Hệ thống kết hợp khả năng thu thập dữ liệu tự động từ các nguồn bên thứ ba (`vang.today`), lưu trữ cơ sở dữ liệu quan hệ PostgreSQL tối ưu hóa, và giao diện người dùng tương tác trực quan với biểu đồ thời gian thực.

Dự án được xây dựng theo chuẩn **Clean / Layered Architecture** cho Backend ASP.NET Core Web API và **Component-Driven Standalone Architecture** với **Angular Signals** cho Frontend.

---

## ✨ Tính Năng Nổi Bật (Key Features)

- 🔄 **Đồng bộ thời gian thực (Real-time Sync):** Lấy giá vàng mới nhất chỉ với 1 cú click chuột thông qua External Provider API.
- 📅 **Tự động tải lịch sử (Auto-Sync History):** Tự động đồng bộ dữ liệu giá vàng 7 ngày gần nhất khi mở trang web để biểu đồ luôn có đầy đủ dữ liệu quá khứ.
- 🛡️ **Khử trùng lặp thông minh (Smart Deduplication):** Thuật toán đối soát dữ liệu đa điều kiện trước khi ghi vào Database, đảm bảo tính Idempotent và không bao giờ vi phạm ràng buộc khóa duy nhất (`Unique Constraint`).
- 📊 **Biểu đồ trực quan (Interactive Line Chart):** Trực quan hóa giá Mua và giá Bán theo thời gian thực sử dụng **Chart.js**, mượt mà và tự động cập nhật khi có dữ liệu mới.
- 📋 **Quản lý dữ liệu toàn diện (Full CRUD):** Xem danh sách lịch sử, thêm bản ghi thủ công, chỉnh sửa thông tin giá và xóa bản ghi với modal xác nhận an toàn.
- 🌙 **Giao diện Dark Gold Theme:** Phong cách tối sang trọng với điểm nhấn vàng ánh kim, thiết kế Responsive hiển thị hoàn hảo trên cả điện thoại di động và máy tính để bàn.
- ⚡ **Zero-CORS Issue:** Tích hợp Angular Dev Proxy loại bỏ triệt để mọi lỗi liên quan đến CORS và SSL certificate trong môi trường phát triển.

---

## 🏗️ Kiến Trúc Hệ Thống (System Architecture)

```text
                               ┌───────────────────────────────────┐
                               │       Người Dùng / Trình Duyệt    │
                               └─────────────────┬─────────────────┘
                                                 │ HTTP
                                                 ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                       FRONTEND (ANGULAR)                                         │
│                                                                                                  │
│   DashboardComponent ──► PriceChartComponent (Chart.js) ──► GoldListComponent (CRUD Modal)       │
│           │                                                                                      │
│           ▼ (Signals & Services)                                                                 │
│      GoldService ────────► ErrorInterceptor ────────► Reverse Proxy (proxy.conf.json)            │
└────────────────────────────────────────────────┬─────────────────────────────────────────────────┘
                                                 │ Forward /api/* (Port 5248)
                                                 ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                  BACKEND (ASP.NET CORE WEB API)                                  │
│                                                                                                  │
│   Controllers Layer        : GoldController                                                      │
│   Business Layer           : GoldPriceService (IGoldPriceService)                                │
│   External Provider Layer  : VangTodayGoldPriceProvider (Typed HttpClient)                       │
│   Data Persistence Layer   : ApplicationDbContext (EF Core 10 + Npgsql)                          │
└────────────────────────────────────────────────┬─────────────────────────────────────────────────┘
                                                 │
                                ┌────────────────┴────────────────┐
                                ▼                                 ▼
                     ┌──────────────────────┐         ┌────────────────────────┐
                     │ PostgreSQL (Docker)  │         │  External Gold API     │
                     │  Table: GoldPrices   │         │     (vang.today)       │
                     └──────────────────────┘         └────────────────────────┘
```

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

### **Backend (.NET 10)**
- **Framework:** ASP.NET Core Web API (.NET 10)
- **ORM:** Entity Framework Core 10 (Code-First & Migrations)
- **Database Driver:** `Npgsql.EntityFrameworkCore.PostgreSQL`
- **HTTP Client:** Typed HttpClient với `IHttpClientFactory`
- **Serializer:** `System.Text.Json`

### **Frontend (Angular)**
- **Framework:** Angular (v17+) với **Standalone Components**
- **State Management:** **Angular Signals** (`signal`, Fine-grained reactivity)
- **Data Visualization:** **Chart.js** (Tree-shaken modules)
- **Styling:** SCSS, CSS Custom Properties (Variables), Responsive CSS Grid & Flexbox

### **Hạ Tầng & Cơ Sở Dữ Liệu**
- **Database:** PostgreSQL chạy trên Docker container
- **Dev Tooling:** Angular Reverse Proxy, PowerShell Automation Scripts

---

## 📋 Yêu Cầu Cài Đặt (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt:
1. **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (hoặc .NET 8/9 trở lên)
2. **[Node.js](https://nodejs.org/)** (v20+ hoặc v24+) & **npm**
3. **[Docker Desktop](https://www.docker.com/)** (đang chạy service Docker)
4. **Angular CLI** (tùy chọn, cài qua: `npm install -g @angular/cli`)

---

## 🚀 Hướng Dẫn Khởi Chạy (Quick Start)

### Cách 1: Khởi động 1-Click (Khuyên dùng trên Windows)
Dự án đã tích hợp sẵn script tự động kiểm tra Docker, chạy Database, Backend và Frontend cùng lúc:
- **Cách đơn giản nhất:** Click đúp chuột vào file **`start-dev.bat`** tại thư mục gốc.
- **Hoặc chạy qua PowerShell:**
  ```powershell
  .\start-dev.ps1
  ```
*Script sẽ tự động khởi động PostgreSQL container, mở 2 cửa sổ terminal cho Backend & Frontend và tự động mở trình duyệt `http://localhost:4200`.*

---

### Cách 2: Khởi chạy thủ công từng phần

#### 1. Khởi động PostgreSQL bằng Docker
```bash
docker run --name goldwatch-postgres -e POSTGRES_USER=goldwatch -e POSTGRES_PASSWORD=goldwatch_dev -e POSTGRES_DB=goldwatch -p 5432:5432 -d postgres
```

#### 2. Khởi động Backend API
```bash
cd GoldWatch.Api

# Cập nhật migrations vào database (lần đầu chạy)
dotnet ef database update

# Khởi chạy Backend trên cổng HTTP (5248)
dotnet run --launch-profile http
```
> API sẽ lắng nghe tại: `http://localhost:5248`

#### 3. Khởi động Frontend Angular
```bash
cd goldwatch-angular

# Cài đặt thư viện (nếu chưa cài)
npm install

# Khởi chạy dev server kèm cấu hình Proxy
npm start
```
> Ứng dụng sẽ mở tại: `http://localhost:4200`

---

## 📡 Danh Sách API Endpoints (Backend API)

Base route: `/api/gold`

| Phương thức | Endpoint | Mô tả chức năng | Body / Params |
|---|---|---|---|
| `GET` | `/api/gold` | Lấy toàn bộ danh sách lịch sử giá vàng trong DB | Không có |
| `GET` | `/api/gold/{id}` | Lấy chi tiết một bản ghi theo ID | `id` (int) |
| `POST` | `/api/gold` | Tạo bản ghi giá vàng thủ công | JSON: `CreateGoldPriceRequest` |
| `PUT` | `/api/gold/{id}` | Chỉnh sửa thông tin bản ghi | JSON: `CreateGoldPriceRequest` |
| `DELETE` | `/api/gold/{id}` | Xóa một bản ghi theo ID | `id` (int) |
| `POST` | `/api/gold/sync` | Lấy và lưu giá vàng thời gian thực mới nhất | Query: `?type={goldType}` (tùy chọn) |
| `POST` | `/api/gold/sync-history` | Đồng bộ dữ liệu lịch sử nhiều ngày qua | Query: `?days=7&type={goldType}` |

---

## 📁 Cấu Trúc Thư Mục Dự Án (Project Structure)

```text
GoldWatch/
├── GoldWatch.Api/                        # ASP.NET Core Web API Project
│   ├── Controllers/                      # RESTful API Controllers
│   │   └── GoldController.cs
│   ├── Data/                             # Entity Framework Core Context
│   │   └── ApplicationDbContext.cs
│   ├── DTOs/                             # Data Transfer Objects
│   │   ├── CreateGoldPriceRequest.cs
│   │   └── Externals/                    # External API DTOs (vang.today)
│   ├── Models/                           # Domain Entities (GoldPrice)
│   ├── Services/                         # Business Logic & Integrations
│   │   ├── GoldPriceService.cs
│   │   └── Externals/                    # Third-party API Providers
│   ├── Program.cs                        # App entry point & DI configuration
│   └── appsettings.json                  # Database connection & API settings
│
├── goldwatch-angular/                    # Angular Frontend Project
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                     # Services, Models, Error Interceptor
│   │   │   └── features/                 # UI Features (Dashboard, Chart, List)
│   │   ├── environments/                 # Environment config (Proxy endpoints)
│   │   └── styles.scss                   # Global Dark Gold Design System
│   ├── proxy.conf.json                   # Reverse Proxy chống lỗi CORS
│   └── package.json
│
├── Flow markdown/                        # Tài liệu chi tiết kiến trúc & kỹ thuật
│   ├── 01_Backend_Architecture_and_System_Design.md
│   ├── 02_CSharp_DotNet_EFCore_DeepDive.md
│   └── 03_Frontend_Angular_Architecture_and_Techniques.md
│
├── start-dev.bat                         # File click đúp chạy nhanh hệ thống
├── start-dev.ps1                         # PowerShell Script điều phối khởi chạy
└── README.md                             # Tài liệu tổng quan dự án
```

---

## 📚 Tài Liệu Kỹ Thuật Chi Tiết (Documentation)

Để tìm hiểu sâu hơn về từng thành phần trong dự án, bạn có thể đọc các tài liệu tại thư mục `Flow markdown/`:
- 🏛️ [Kiến Trúc Hệ Thống & Thiết Kế Phần Mềm](Flow%20markdown/01_Backend_Architecture_and_System_Design.md)
- ⚙️ [C#, .NET & Entity Framework Core Chuyên Sâu](Flow%20markdown/02_CSharp_DotNet_EFCore_DeepDive.md)
- 🎨 [Frontend Angular Architecture & Techniques](Flow%20markdown/03_Frontend_Angular_Architecture_and_Techniques.md)

---

## 📄 Bản Quyền (License)

Dự án được phân phối dưới giấy phép **MIT License**. Bạn hoàn toàn có thể tự do sử dụng, chỉnh sửa và phát triển cho mục đích học tập hoặc thương mại.

---
*Developed with ❤️ using .NET 10, Angular & PostgreSQL.*

