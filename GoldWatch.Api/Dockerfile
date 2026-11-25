FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies trước (tận dụng Docker layer cache)
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ source code
COPY . ./

# Publish ở Release mode
RUN dotnet publish -c Release -o /app/publish

# --- Runtime image (nhỏ hơn SDK image) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

# Render dùng port 10000; local docker-compose ghi đè qua ASPNETCORE_URLS
EXPOSE 5248 10000

ENTRYPOINT ["./entrypoint.sh"]
