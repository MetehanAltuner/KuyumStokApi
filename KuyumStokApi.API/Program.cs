using KuyumStokApi.Infrastructure;
using KuyumStokApi.Infrastructure.Services.BanksService;
using KuyumStokApi.Persistence;
using KuyumStokApi.Infrastructure.DevSeeding;
using KuyumStokApi.Persistence.Extensions;
using KuyumStokApi.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Text.Json;
using KuyumStokApi.Application.Interfaces.Auth;
using KuyumStokApi.Application.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using KuyumStokApi.API.Converters;
static byte[] DecodeKey(string? b64)
{
    if (string.IsNullOrWhiteSpace(b64))
        throw new InvalidOperationException("Jwt:Key boş!");

    var clean = b64.Trim();
    return Convert.FromBase64String(clean);
}

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Tüm DateTime ve DateTimeOffset değerlerini "yyyy-MM-dd HH:mm:ss" formatında UTC olarak serialize et
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeOffsetJsonConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var payload = ApiResult<object>.Fail("Doğrulama hatası.", errors, statusCode: 400);
            return new BadRequestObjectResult(payload);
        };
    });
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        // SignalR için de aynı DateTime formatını kullan
        options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.PayloadSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
        options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
        options.PayloadSerializerOptions.Converters.Add(new NullableUtcDateTimeOffsetJsonConverter());
    });

// CORS yapılandırması (SignalR için gerekli)
// DEV: Tüm origin'lere izin ver (production'da spesifik origin'ler belirtilmeli)
builder.Services.AddCors(options =>
{
    // SignalR için özel policy
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Tüm origin'lere izin ver
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // SignalR için credentials gerekli
    });
    
    // Default policy (diğer endpoint'ler için)
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddPersistence(cfg);
builder.Services.AddInfrastructure(cfg);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keyBytes = DecodeKey(cfg["Jwt:Key"]);                // <-- helper ile çöz
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = cfg["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes), // <-- aynı baytlar
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.IncludeErrorDetails = true; // detay gelsin

        // SignalR için JWT authentication desteği
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] AuthFailed: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // 401 sebebi
                Console.WriteLine($"[JWT] Challenge: {ctx.Error} - {ctx.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                // Token blacklist kontrolü
                var jtiClaim = ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
                if (jtiClaim != null)
                {
                    var tokenBlacklistService = ctx.HttpContext.RequestServices.GetRequiredService<KuyumStokApi.Application.Interfaces.Services.ITokenBlacklistService>();
                    var isInvalidated = await tokenBlacklistService.IsTokenInvalidatedAsync(jtiClaim.Value);
                    if (isInvalidated)
                    {
                        ctx.Fail("Token geçersiz kılınmış (logout yapılmış).");
                        return;
                    }
                }
                Console.WriteLine("[JWT] Token validated.");
            },
            OnMessageReceived = context =>
            {
                // SignalR için query string'den token al
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/hubs"))
                {
                    context.Token = accessToken;
                }
                // Authorization header'dan da al
                else if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KuyumStokApi", Version = "v1" });

    // 1) Şemayı tanımla
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Sadece JWT'yi yapıştırın. 'Bearer ' yazmayın.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // 2) TÜM operasyonlara global requirement uygula
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // <-- Şema adını REFERANSLA
                }
            },
            Array.Empty<string>()
        }
    });

    // XML doc dosyaları: çalışmakta olan assembly konumundan .xml’i bul
    static string? XmlOf(Type t)
        => System.IO.File.Exists(System.IO.Path.ChangeExtension(t.Assembly.Location, ".xml"))
           ? System.IO.Path.ChangeExtension(t.Assembly.Location, ".xml")
           : null;

    var apiXml = XmlOf(typeof(Program));
    var appXml = XmlOf(typeof(ApiResult<>));          // Application
    var infraXml = XmlOf(typeof(BanksService));         // Infrastructure

    foreach (var xml in new[] { apiXml, appXml, infraXml })
        if (xml is not null) c.IncludeXmlComments(xml, includeControllerXmlComments: true);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS (Authentication'dan önce olmalı)
app.UseCors();

// Static files (test HTML sayfası için)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR hub'ına özel CORS policy uygula
app.MapHub<DashboardHub>("/api/hubs/dashboard")
   .RequireCors("SignalRCorsPolicy");

// 🔄 Veritabanı migration ve seed data (app.Run öncesi!)
await app.MigrateAndSeedAsync();

// Development-only transactional seed (profit/loss demo)
await DevSeedRunner.RunAsync(app.Services, app.Environment, app.Configuration);

app.Run();
