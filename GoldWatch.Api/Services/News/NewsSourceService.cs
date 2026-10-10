using GoldWatch.Api.Data;
using GoldWatch.Api.DTOs;
using GoldWatch.Api.Models;
using Microsoft.EntityFrameworkCore;


namespace GoldWatch.Api.Services.News;


public class NewsSourceService : INewsSourceService
{
    private readonly ApplicationDbContext _context;

    public NewsSourceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NewsSource>> GetAllAsync()
    {
        return await _context.NewsSources.AsNoTracking().ToListAsync();
    }

    // Lấy chỉ các nguồn đang được BẬT (phục vụ cho Collector ở Chặng 4.3)
    public async Task<IReadOnlyList<NewsSource>> GetActiveSourcesAsync()
    {
        return await _context.NewsSources.AsNoTracking().Where(s => s.IsActive == true).ToListAsync();
    }

    public async Task<NewsSource?> GetByIdAsync(int id)
    {
        return await _context.NewsSources.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<NewsSource> CreateAsync(CreateNewsSourceRequest request)
    {
        var source = new NewsSource
        {
            Name = request.Name,
            Url = request.Url,
            RssUrl = request.RssUrl,
            SourceType = request.SourceType,
            IsActive = request.IsActive
        };

        _context.NewsSources.Add(source);
        await _context.SaveChangesAsync();

        return source;
    }

    public async Task<bool> UpdateAsync(int id, UpdateNewsSourceRequest request)
    {
        var source = await _context.NewsSources.FirstOrDefaultAsync(x => x.Id == id);

        if (source is null)
        {
            return false;
        }

        source.Name = request.Name;
        source.Url = request.Url;
        source.RssUrl = request.RssUrl;
        source.SourceType = request.SourceType;
        source.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var source = await _context.NewsSources.FirstOrDefaultAsync(x => x.Id == id);

        if (source is null)
        {
            return false;
        }

        _context.Remove(source);

        await _context.SaveChangesAsync();
        return true;
    }
}