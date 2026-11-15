using Microsoft.EntityFrameworkCore;
using StudentWorkload.Application.Interfaces;
using StudentWorkload.Application.Services;
using StudentWorkload.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using student_workload_monitor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Регистрация базы данных AppDbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Подключение контроллеров и форматирование ошибок валидации в JSON (400 Bad Request)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path
            };
            return new BadRequestObjectResult(problemDetails);
        };
    });

// Регистрация глобального перехватчика необработанных исключений
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IWorkloadForecastingService, WorkloadForecastingService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Автоматическое создание таблиц и применение миграций БД при запуске приложения
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Включение middleware для перехвата ошибок в конвейере запросов
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Открытие точки входа приложения для запуска интеграционных тестов
public partial class Program { }