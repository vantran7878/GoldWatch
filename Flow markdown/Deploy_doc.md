# 🚀 GoldWatch — Hướng dẫn Deploy Full Stack

> **Stack:** Angular 22 (Vercel) · ASP.NET Core 10 (Render) · PostgreSQL (Supabase)

---

## Tổng quan kiến trúc

```mermaid
flowchart LR
    USER["👤 User"] --> FE["Vercel\nAngular Frontend"]
    FE -->|HTTPS API calls| BE["Render\nASP.NET Core API"]
    BE -->|PostgreSQL| DB["Supabase\nPostgreSQL"]
    GH["GitHub\nmain branch"] -->|push| CI1["GitHub Actions\ndeploy-frontend"]
    GH -->|push| CI2["GitHub Actions\ndeploy-backend"]
    CI1 --> FE
    CI2 -->|Deploy Hook| BE
```

---

## Bước 1 — Database: Supabase

### 1.1 Tạo project Supabase
1. Vào [supabase.com](https://supabase.com) → **New Project**
2. Điền tên project: `goldwatch`, chọn region **Southeast Asia (Singapore)**
3. Đặt mật khẩu DB mạnh, lưu lại

### 1.2 Lấy Connection String
1. Vào **Project Settings → Database → Connection string**
2. Chọn tab **URI**, copy chuỗi dạng:
   ```
   postgresql://postgres.[PROJECT_REF]:[PASSWORD]@aws-0-ap-southeast-1.pooler.supabase.com:5432/postgres
   ```
3. **Lưu lại** — sẽ dùng ở bước Render

> [!NOTE]
> Supabase free tier giới hạn 500MB storage và pause project sau 7 ngày không hoạt động. Dùng **Supabase CLI** (`supabase db push`) hoặc API cron để giữ active.

---

## Bước 2 — Backend: Render

### 2.1 Kết nối GitHub với Render
1. Vào [render.com](https://render.com) → **New → Web Service**
2. Chọn **Connect a repository** → chọn repo `GoldWatch`
3. Cấu hình:

| Field | Giá trị |
|-------|---------|
| **Name** | `goldwatch-api` |
| **Region** | Singapore |
| **Runtime** | **Docker** |
| **Dockerfile Path** | `./GoldWatch.Api/Dockerfile` |
| **Docker Context** | `./GoldWatch.Api` |
| **Branch** | `main` |
| **Plan** | Free (hoặc Starter \$7/tháng để không ngủ) |

### 2.2 Cài Environment Variables trên Render
Vào **Environment → Add Environment Variable**:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:10000` |
| `ConnectionStrings__DefaultConnection` | *(Supabase URI từ bước 1.2)* |

> [!IMPORTANT]
> Render web services luôn chạy trên **port 10000** — `ASPNETCORE_URLS=http://+:10000` là bắt buộc.

### 2.3 Lấy Deploy Hook URL
1. Render Dashboard → Service → **Settings → Deploy Hook**
2. Copy URL dạng: `https://api.render.com/deploy/srv-xxx?key=yyy`
3. Thêm vào GitHub Secrets: `RENDER_DEPLOY_HOOK_URL`

### 2.4 Lưu ý Free Tier Render
- Free tier sẽ **spin down sau 15 phút** không có request → cold start ~30s
- Nâng lên **Starter (\$7/tháng)** để luôn chạy

---

## Bước 3 — Frontend: Vercel

### 3.1 Cập nhật API URL
Mở file [`src/environments/environment.production.ts`](file:///home/van/dev/DotNet/GoldWatch/goldwatch-angular/src/environments/environment.production.ts) và thay:
```ts
apiUrl: 'https://YOUR_RENDER_APP.onrender.com/api'
```
→ Thay `YOUR_RENDER_APP` bằng tên service thực tế trên Render (ví dụ: `goldwatch-api`).

### 3.2 Deploy lên Vercel
**Cách A — Import từ GitHub (đơn giản nhất):**
1. Vào [vercel.com](https://vercel.com) → **Add New → Project**
2. Import repo GitHub `GoldWatch`
3. Cấu hình:

| Field | Giá trị |
|-------|---------|
| **Framework Preset** | `Angular` |
| **Root Directory** | `goldwatch-angular` |
| **Build Command** | `npm run build` |
| **Output Directory** | `dist/goldwatch-angular/browser` |

4. Click **Deploy** ✅

**Cách B — GitHub Actions (tự động):**
1. Cài [Vercel CLI](https://vercel.com/docs/cli): `npm i -g vercel`
2. Chạy trong `goldwatch-angular/`: `vercel link` → lấy Project ID và Org ID
3. Thêm vào GitHub Secrets:

| Secret | Lấy từ |
|--------|--------|
| `VERCEL_TOKEN` | vercel.com → Account Settings → Tokens |
| `VERCEL_ORG_ID` | `vercel link` output hoặc `.vercel/project.json` |
| `VERCEL_PROJECT_ID` | `.vercel/project.json` |

### 3.3 Cấu hình SPA Routing (đã có sẵn)
File [`vercel.json`](file:///home/van/dev/DotNet/GoldWatch/goldwatch-angular/vercel.json) đã được tạo để redirect tất cả routes về `index.html`.

### 3.4 Cập nhật CORS trên Backend
Sau khi có domain Vercel (ví dụ: `https://goldwatch.vercel.app`), Render tự inject `ASPNETCORE_URLS` xử lý CORS vì code hiện tại dùng `AllowAngularDev` chỉ cho phép `localhost`. Thêm environment variable trên Render:

| Key | Value |
|-----|-------|
| `AllowedOrigins__0` | `https://goldwatch.vercel.app` |

Và cập nhật `Program.cs`:
```csharp
// Thêm sau builder.Services.AddCors(...)
// Hoặc sửa policy AllowAngularDev để đọc từ config:
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                var uri = new Uri(origin);
                return uri.Host == "localhost"
                    || uri.Host == "127.0.0.1"
                    || allowedOrigins.Contains(origin);
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

---

## Bước 4 — GitHub Actions CI/CD

Hai workflow đã được tạo tự động:

| File | Trigger | Tác dụng |
|------|---------|----------|
| [`deploy-frontend.yml`](file:///home/van/dev/DotNet/GoldWatch/.github/workflows/deploy-frontend.yml) | push `main` → `goldwatch-angular/**` | Build & deploy Angular lên Vercel |
| [`deploy-backend.yml`](file:///home/van/dev/DotNet/GoldWatch/.github/workflows/deploy-backend.yml) | push `main` → `GoldWatch.Api/**` | Trigger Render deploy hook |

### Secrets cần thêm vào GitHub repo:
Vào repo → **Settings → Secrets and variables → Actions → New repository secret**

| Secret | Dùng cho |
|--------|---------|
| `VERCEL_TOKEN` | Frontend deploy |
| `VERCEL_ORG_ID` | Frontend deploy |
| `VERCEL_PROJECT_ID` | Frontend deploy |
| `RENDER_DEPLOY_HOOK_URL` | Backend deploy |

---

## Checklist Deploy

- [ ] **Supabase**: Tạo project, copy connection string
- [ ] **Render**: Tạo web service, set env vars (đặc biệt `ConnectionStrings__DefaultConnection` và `ASPNETCORE_URLS=http://+:10000`)
- [ ] **Render Deploy Hook**: Copy URL → thêm vào GitHub Secrets
- [ ] **environment.production.ts**: Thay `YOUR_RENDER_APP` bằng tên thật
- [ ] **CORS**: Thêm Vercel domain vào `AllowedOrigins` trên Render
- [ ] **Vercel**: Import project, set Root Directory = `goldwatch-angular`
- [ ] **GitHub Secrets**: Thêm đủ 4 secrets
- [ ] **Test**: Push lên `main`, kiểm tra cả 2 GitHub Actions chạy xanh ✅

---

## Cấu trúc files đã tạo/sửa

```
GoldWatch/
├── render.yaml                                      ← Render blueprint (optional)
├── docker-compose.yml                               ← Local dev (đã sửa)
├── .github/workflows/
│   ├── deploy-frontend.yml                          ← CI/CD Angular → Vercel
│   └── deploy-backend.yml                           ← CI/CD API → Render
├── GoldWatch.Api/
│   ├── Dockerfile                                   ← Multi-stage, đã sửa lỗi
│   ├── entrypoint.sh                               ← Wait for PG, run app
│   └── Program.cs                                  ← db.Database.Migrate() on startup
└── goldwatch-angular/
    ├── vercel.json                                  ← SPA routing fix
    ├── public/_redirects                            ← (backup cho Netlify nếu cần)
    └── src/environments/
        ├── environment.ts                           ← Dev: apiUrl = '/api'
        └── environment.production.ts               ← Prod: Render URL
```
