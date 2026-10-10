using System.Runtime.CompilerServices;
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;

namespace GoldWatch.Api.Services.News;

public interface INewsSourceService
{
    Task<IReadOnlyList<NewsSource>> GetAllAsync();

    Task<IReadOnlyList<NewsSource>> GetActiveSourcesAsync();
    Task<NewsSource?> GetByIdAsync(int id);

    Task<NewsSource> CreateAsync(CreateNewsSourceRequest request);

    Task<bool> UpdateAsync(int id, UpdateNewsSourceRequest request);
    Task<bool> DeleteAsync(int id);
}