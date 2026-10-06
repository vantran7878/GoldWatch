using GoldWatch.Api.Controllers;
using GoldWatch.Api.Data;
using Microsoft.EntityFrameworkCore;
using GoldWatch.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new
InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<ApplicationDbContext>(options
                => options.UseNpgsql(connectionString));
// ApplicationDbContext được đăng ký với vòng đời "Scoped"


var app = builder.Build();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IGoldPriceService, GoldPriceService>();
builder.Services.AddControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map route của các Controller vào pipeline
app.MapControllers(); // <-- THÊM DÒNG NÀY

app.Run();