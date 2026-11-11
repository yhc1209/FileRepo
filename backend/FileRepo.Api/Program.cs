using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FileRepo.Api.Data;
using FileRepo.Api.Models;
using FileRepo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileRepo API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "請在下方輸入 JWT Bearer Token (格式: Bearer {token})",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var dbPath = builder.Configuration["DbPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "filerepo.db");
var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
{
    Directory.CreateDirectory(dbDir);
}

builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Auth & Security Services
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "FileRepo_Super_Secret_Key_For_Jwt_Validation_2025_Min_32_Bytes!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FileRepo.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FileRepo.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Range", "Content-Length", "Accept-Ranges");
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    db.Database.EnsureCreated();

    // 如果現有 SQLite 資料庫是先前 EnsureCreated 建立（當時尚未有 Users 資料表），
    // 補足 Users 資料表及索引，避免使用者在已存在的 db 上註冊時拋出 "no such table: Users"
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Users"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""Email"" TEXT NOT NULL,
            ""PasswordHash"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL,
            ""IsTwoFactorEnabled"" INTEGER NOT NULL,
            ""TwoFactorType"" INTEGER NOT NULL,
            ""TwoFactorSecret"" TEXT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
    ");
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
