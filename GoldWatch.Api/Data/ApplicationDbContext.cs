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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Đảm bảo không thể có 2 bản ghi cùng loại vàng, cùng nguồn và cùng thời gian
        modelBuilder.Entity<GoldPrice>().HasIndex(x => new { x.GoldType, x.Source, x.CollectedAt }).IsUnique();
    }
}