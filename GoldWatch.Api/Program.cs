using GoldWatch.Api.Controllers;
using GoldWatch.Api.Data;
using Microsoft.EntityFrameworkCore;
using GoldWatch.Api.Services;
using GoldWatch.Api.Services.Externals;
using GoldWatch.Api.Services.News;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new
InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                var uri = new Uri(origin);
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options
                => options.UseNpgsql(connectionString));
// ApplicationDbContext được đăng ký với vòng đời "Scoped"

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IGoldPriceService, GoldPriceService>();
builder.Services.AddScoped<INewsSourceService, NewsSourceService>();
builder.Services.AddControllers();


// Register with Typed: HttpClient for VangTodayGoldPriceProvider

builder.Services.AddHttpClient<IGoldPriceProvider, VangTodayGoldPriceProvider>(
    client =>
    {
        var baseUrl = builder.Configuration["GoldPriceApi:BaseUrl"] ?? "https://www.vang.today/api/";
        client.BaseAddress = new Uri(baseUrl);

        var timeoutSeconds = builder.Configuration.GetValue<int>("GoldPriceApi:TimeoutSeconds", 10);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }
);



var app = builder.Build();

// Apply pending EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngularDev");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Map route của các Controller vào pipeline
app.MapControllers(); // <-- THÊM DÒNG NÀY

app.Run();