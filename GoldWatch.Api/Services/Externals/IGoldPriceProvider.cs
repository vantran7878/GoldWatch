using GoldWatch.Api.Models;

namespace GoldWatch.Api.Services.Externals;


public interface IGoldPriceProvider
{
    Task<GoldPrice> FetchLatestPriceAsync(string? goldPrice = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldPrice>> FetchPriceHistoryAsync(
        string? goldType = null,
        int days = 7,
        CancellationToken cancellationToken = default
    );
}