namespace GoldWatch.Api.Services;

using GoldWatch.Api.Models;
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Data;
using Microsoft.EntityFrameworkCore;

public class GoldPriceService : IGoldPriceService
{
    private readonly ApplicationDbContext _context;

    public GoldPriceService(ApplicationDbContext context)
    {
        _context = context;
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
}

