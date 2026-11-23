# GoldWatch - Fullstack Startup Script (BE + FE + DB)

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "    Starting GoldWatch Fullstack System       " -ForegroundColor Yellow
Write-Host "==============================================" -ForegroundColor Cyan

# 1. Kiểm tra Docker PostgreSQL
Write-Host "`n[1/3] Kiem tra Docker PostgreSQL..." -ForegroundColor Green
$pgStatus = docker inspect -f '{{.State.Running}}' goldwatch-postgres 2>$null
if ($pgStatus -ne "true") {
    Write-Host "Khoi dong container goldwatch-postgres..." -ForegroundColor Yellow
    docker start goldwatch-postgres
} else {
    Write-Host "PostgreSQL dang chay tren port 5432 (OK)" -ForegroundColor DarkGreen
}

# 2. Khoi dong Backend (ASP.NET Core Web API) tren cua so rieng
Write-Host "`n[2/3] Khoi dong Backend (GoldWatch.Api) tren port 5248..." -ForegroundColor Green
$bePath = Join-Path $PSScriptRoot "GoldWatch.Api"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$bePath'; Write-Host '--- GOLDWATCH BACKEND API (http://localhost:5248) ---' -ForegroundColor Cyan; dotnet run --launch-profile http"

# 3. Khoi dong Frontend (Angular) tren cua so rieng va mo browser
Write-Host "`n[3/3] Khoi dong Frontend (Angular) tren port 4200..." -ForegroundColor Green
$fePath = Join-Path $PSScriptRoot "goldwatch-angular"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$fePath'; Write-Host '--- GOLDWATCH ANGULAR FRONTEND (http://localhost:4200) ---' -ForegroundColor Yellow; ng serve --open"

Write-Host "`n==============================================" -ForegroundColor Cyan
Write-Host " Tat ca dich vu da duoc khoi dong thanh cong! " -ForegroundColor Green
Write-Host " - Backend API:  http://localhost:5248        " -ForegroundColor White
Write-Host " - Frontend UI:  http://localhost:4200        " -ForegroundColor White
Write-Host " - Database:     PostgreSQL (localhost:5432)  " -ForegroundColor White
Write-Host "==============================================" -ForegroundColor Cyan

