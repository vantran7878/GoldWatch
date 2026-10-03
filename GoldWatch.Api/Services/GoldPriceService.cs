using GoldWatch.Api.Services;
using GoldWatch.Api.Models;
using GoldWatch.Api.DTOs;

public class GoldPriceService : IGoldPriceService
{
    private readonly List<GoldPrice> _goldPrices = new();

    public IReadOnlyList<GoldPrice> GetAll()
    {
        return _goldPrices;
    }

    public GoldPrice? GetByID(int id)
    {
        GoldPrice? price = _goldPrices.FirstOrDefault(x => x.Id == id);
        return price;
    }

    public GoldPrice Create(CreateGoldPriceRequest request)
    {
        var goldPrice = new GoldPrice
        {
            Id = _goldPrices.Count + 1,
            GoldType = request.GoldType,
            BuyPrice = request.BuyPrice,
            SellPrice = request.SellPrice,
            Currency = request.Currency,
            Source = request.Source
        };

        _goldPrices.Add(goldPrice);

        return goldPrice;
    }

    public bool Update(int id, CreateGoldPriceRequest request)
    {
        var goldPrice = GetByID(id);

        if (goldPrice is null)
        {
            return false;
        }

        goldPrice.GoldType = request.GoldType;
        goldPrice.BuyPrice = request.BuyPrice;
        goldPrice.SellPrice = request.SellPrice;
        goldPrice.Currency = request.Currency;
        goldPrice.Currency = request.Source;

        return true;
    }

    public bool Delete(int id)
    {
        var goldPrice = GetByID(id);

        if (goldPrice is null)
        {
            return false;
        }

        _goldPrices.Remove(goldPrice);
        return true;
    }
}

