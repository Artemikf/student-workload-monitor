using Microsoft.EntityFrameworkCore;
using StudentWorkload.Application.Interfaces;
using StudentWorkload.Application.Services;
using StudentWorkload.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Регистрация базы данных AppDbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IWorkloadForecastingService, WorkloadForecastingService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();