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
}