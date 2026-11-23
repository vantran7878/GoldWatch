namespace GoldWatch.Api.Services;

using GoldWatch.Api.Models;
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Data;
using GoldWatch.Api.Services.Externals;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

public class GoldPriceService : IGoldPriceService
{
    private readonly ApplicationDbContext _context;

    private readonly IGoldPriceProvider _goldPriceProvider;

    public GoldPriceService(ApplicationDbContext context, IGoldPriceProvider goldPriceProvider)
    {
        _context = context;
        _goldPriceProvider = goldPriceProvider;

    }

    public async Task<IReadOnlyList<GoldPrice>> GetAllAsync()
    {
        return await _context.GoldPrices.AsNoTracking().ToListAsync();
    }

    public async Task<GoldPrice?> GetByIDAsync(int id)
    {
        GoldPrice? price = await _context.GoldPrices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return price;
    }

    public async Task<GoldPrice> CreateAsync(CreateGoldPriceRequest request)
    {
        var goldPrice = new GoldPrice
        {
            GoldType = request.GoldType,
            BuyPrice = request.BuyPrice,
            SellPrice = request.SellPrice,
            Currency = request.Currency,
            Source = request.Source,
            CollectedAt = DateTime.UtcNow
        };

        _context.GoldPrices.Add(goldPrice);

        await _context.SaveChangesAsync();

        return goldPrice;
    }

    public async Task<bool> UpdateAsync(int id, CreateGoldPriceRequest request)
    {
        var goldPrice = await _context.GoldPrices.FirstOrDefaultAsync(x => x.Id == id);

        if (goldPrice is null)
        {
            return false;
        }

        goldPrice.GoldType = request.GoldType;
        goldPrice.BuyPrice = request.BuyPrice;
        goldPrice.SellPrice = request.SellPrice;
        goldPrice.Currency = request.Currency;
        goldPrice.Source = request.Source;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var goldPrice = await _context.GoldPrices.FirstOrDefaultAsync(x => x.Id == id);

        if (goldPrice is null)
        {
            return false;
        }

        _context.GoldPrices.Remove(goldPrice);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<GoldPrice> SyncLatestPriceAsync(string? goldType = null, CancellationToken cancellationToken = default)
    {
        // 1. Gọi external provider lấy giá vàng từ internet
        var goldPrice = await _goldPriceProvider.FetchLatestPriceAsync(goldType, cancellationToken);


        // Check if the fetched exist and the duplicate with database, return and do not add.
        var existing = await _context.GoldPrices.FirstOrDefaultAsync(x => x.GoldType == goldPrice.GoldType
        && x.Source == goldPrice.Source
        && x.CollectedAt == goldPrice.CollectedAt,
        cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        // 2. Thêm vào DbSet và lưu xuống PostgreSQL
        _context.GoldPrices.Add(goldPrice);
        await _context.SaveChangesAsync(cancellationToken);

        return goldPrice;
    }

    public async Task<IReadOnlyList<GoldPrice>> SyncPriceHistoryAsync(string? goldType = null, int days = 7, CancellationToken cancellationToken = default)
    {
        var historyPrices = await _goldPriceProvider.FetchPriceHistoryAsync(goldType, days, cancellationToken);

        if (historyPrices.Count == 0)
        {
            return historyPrices;
        }

        var datesToCheck = historyPrices.Select(p => p.CollectedAt).ToList();
        var typesToCheck = historyPrices.Select(p => p.GoldType).Distinct().ToList();

        var existingDates = await _context.GoldPrices
            .Where(x => typesToCheck.Contains(x.GoldType)
                     && x.Source == "vang.today"
                     && datesToCheck.Contains(x.CollectedAt))
            .Select(x => x.CollectedAt)
            .ToListAsync(cancellationToken);

        var newPrices = historyPrices.Where(p => !existingDates.Contains(p.CollectedAt)).ToList();

        if (newPrices.Count > 0)
        {
            _context.GoldPrices.AddRange(newPrices);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return historyPrices;
    }
}

