using FileManager.Api.Middlewares;
using FileManager.Core.Configuration;
using FileManager.Core.Entities;
using FileManager.Infrastructure.Data;
using FileManager.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using FileManager.Application.Extensions;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация
builder.Services.Configure<JwtConfig>(
    builder.Configuration.GetSection(JwtConfig.SectionName));
    
builder.Services.Configure<DatabaseConfig>(
    builder.Configuration.GetSection(DatabaseConfig.SectionName));

builder.Services.Configure<LoggingConfig>(
    builder.Configuration.GetSection(LoggingConfig.SectionName));
    
builder.Services.Configure<ExceptionHandlingConfig>(
    builder.Configuration.GetSection(ExceptionHandlingConfig.SectionName));

// 2. База данных
var dbConfig = builder.Configuration
    .GetSection(DatabaseConfig.SectionName)
    .Get<DatabaseConfig>() ?? new DatabaseConfig(); 

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(dbConfig.Default ?? "Data Source=FileManager.db"));

// 3. Регистрация слоёв
builder.Services
    .AddApplicationServices() 
    .AddInfrastructureServices(builder.Configuration);

// Исправление: Регистрируем PasswordHasher вместо Identity Core
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// 4. Аутентификация
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        var jwtConfig = builder.Configuration
            .GetSection(JwtConfig.SectionName)
            .Get<JwtConfig>() ?? new JwtConfig(); 
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig.Secret)),
            NameClaimType = "unique_name",
            RoleClaimType = ClaimTypes.Role 
        };
    });

// 5. Контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

var app = builder.Build();

// 6. Миграции в Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 7. Middleware pipeline
app.UseRouting();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();