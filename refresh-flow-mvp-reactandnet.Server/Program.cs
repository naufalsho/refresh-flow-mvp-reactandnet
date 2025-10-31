using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using refresh_flow_mvp_reactandnet.Server.Controllers;
using refresh_flow_mvp_reactandnet.Server.Data;
using refresh_flow_mvp_reactandnet.Server.Services.Contracts;
using refresh_flow_mvp_reactandnet.Server.Services;
using System.Text;
using refresh_flow_mvp_reactandnet.Server.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
////builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });

    c.AddSecurityDefinition("SeedSecret", new OpenApiSecurityScheme
    {
        Description = "Dev seeding secret",
        Name = "X-Seed-Secret",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.OperationFilter<SeedSecretRequirementFilter>();
});


//appsettings
var cfg = builder.Configuration;

// DB + Identity
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(cfg.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
{
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = true;
    o.SaveToken = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = cfg["Jwt:Issuer"],
        ValidAudience = cfg["Jwt:Audience"],
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DevSeedOptions>(builder.Configuration.GetSection("DevSeed"));

// CORS ketat
builder.Services.AddCors(o =>
{
    o.AddPolicy("frontend", p => p
        .WithOrigins(cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// Cookie policy untuk refresh endpoint
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    // Cookie refresh token akan di-set manual dengan Path /auth/refresh
});

// Rate limiting basic
builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(opt =>
{
    opt.GeneralRules = new()
    {
        new() { Endpoint = "*:/auth/login", Limit = 10, Period = "1m" },
        new() { Endpoint = "*:/auth/refresh", Limit = 20, Period = "1m" }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();



var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//add cors, rate limiting, authentication
app.UseCors("frontend");
// optional: problem-details bawaan (jika pakai .NET 8 minimal API adds)
app.UseStatusCodePages(); // fallback sederhana untuk non-JSON selain 429

app.UseIpRateLimiting();
app.UseSecurityEvents();   // <— log auth & security significant responses

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
