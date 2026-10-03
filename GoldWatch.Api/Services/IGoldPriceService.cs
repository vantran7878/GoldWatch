using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;

namespace GoldWatch.Api.Services;

public interface IGoldPriceService
{
    IReadOnlyList<GoldPrice> GetAll();
    GoldPrice? GetByID(int id);

    GoldPrice Create(CreateGoldPriceRequest request);
    bool Update(int id, CreateGoldPriceRequest request);
    bool Delete(int id);
}