using GoldWatch.Api.Data;
using GoldWatch.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GoldWatch.Api.Data;

public class ApplicationDbContext : DbContext
{
    // Constructor nhận options (chuỗi kết nối, loại DB...) do DI truyền vào
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> option)
    : base(option)
    {
    }


    public DbSet<GoldPrice> GoldPrices => Set<GoldPrice>();
    public DbSet<NewsSource> NewsSources => Set<NewsSource>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Khóa cứng tên bảng số nhiều
        modelBuilder.Entity<NewsSource>().ToTable("NewsSources");
        modelBuilder.Entity<NewsArticle>().ToTable("NewsArticles");

        // Đảm bảo không thể có 2 bản ghi cùng loại vàng, cùng nguồn và cùng thời gian
        modelBuilder.Entity<GoldPrice>().HasIndex(x => new { x.GoldType, x.Source, x.CollectedAt }).IsUnique();

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.HasOne(a => a.Source).WithMany(s => s.Articles).HasForeignKey(a => a.SourceID).OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.Url).IsUnique();
        });
    }
}