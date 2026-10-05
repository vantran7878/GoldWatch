using GoldWatch.Api.Controllers;
using GoldWatch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IGoldPriceService, GoldPriceService>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map route của các Controller vào pipeline
app.MapControllers(); // <-- THÊM DÒNG NÀY

app.Run();