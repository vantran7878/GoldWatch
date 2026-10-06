using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;

namespace GoldWatch.Api.Services;

public interface IGoldPriceService
{
    Task<IReadOnlyList<GoldPrice>> GetAllAsync();
    Task<GoldPrice?> GetByIDAsync(int id);

    Task<GoldPrice> CreateAsync(CreateGoldPriceRequest request);
    Task<bool> UpdateAsync(int id, CreateGoldPriceRequest request);
    Task<bool> DeleteAsync(int id);
}