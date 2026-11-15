using Microsoft.EntityFrameworkCore;
using StudentWorkload.Application.Interfaces;
using StudentWorkload.Application.Services;
using StudentWorkload.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Регистрация базы данных AppDbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IWorkloadForecastingService, WorkloadForecastingService>();

// Заменяем AddOpenApi на AddSwaggerGen
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Настройка конвейера запросов
if (app.Environment.IsDevelopment())
{
    // Подключаем классический Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();