using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;
using GoldWatch.Api.Services.Externals;

namespace GoldWatch.Api.Services.Externals;

public class VangTodayGoldPriceProvider : IGoldPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VangTodayGoldPriceProvider> _logger;

    // HttpClient sẽ được ASP.NET Core DI tự động inject thông qua Typed Client

    public VangTodayGoldPriceProvider(HttpClient httpClient, IConfiguration configuration, ILogger<VangTodayGoldPriceProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoldPrice> FetchLatestPriceAsync(string? goldType = null, CancellationToken cancellationToken = default)
    {
        // 1. Xác định mã loại vàng cần lấy (mặc định lấy từ cấu hình appsettings.json)
        var targetType = goldType ?? _configuration["GoldPriceApi:DefaultType"] ?? "SJL1L10";

        var endpoint = $"prices?type={targetType}";

        _logger.LogInformation($"Calling API vang.today with endpoint {endpoint}");

        // 2. Gửi HTTP GET và tự động Parse JSON sang DTO
        var response = await _httpClient.GetFromJsonAsync<VangTodayPriceResponse>(
            endpoint,
            cancellationToken
        );

        // 3. Kiểm tra tính toàn vẹn của dữ liệu trả về
        if (response is null || !response.Success)
        {
            throw new HttpRequestException($"Cannot take gold prices from vang.today for type: {targetType}");
        }

        // 4. Ánh xạ (Map) từ External DTO sang Domain Model GoldPrice
        return new GoldPrice
        {
            GoldType = string.IsNullOrWhiteSpace(response.Name) ? response.Type : response.Name,
            BuyPrice = response.Buy,
            SellPrice = response.Sell,
            Currency = "VND",
            Source = "vang.today",
            CollectedAt = response.Timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(response.Timestamp).UtcDateTime : DateTime.UtcNow
        };

    }


}