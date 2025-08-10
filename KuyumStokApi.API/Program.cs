using KuyumStokApi.Infrastructure;
using KuyumStokApi.Infrastructure.Services.BanksService;
using KuyumStokApi.Persistence;
using KuyumStokApi.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
static byte[] DecodeKey(string? b64)
{
    if (string.IsNullOrWhiteSpace(b64))
        throw new InvalidOperationException("Jwt:Key boş!");

    var clean = b64.Trim();
    return Convert.FromBase64String(clean);
}

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

builder.Services.AddControllers();

builder.Services.AddPersistence(cfg);
builder.Services.AddInfrastructure(cfg);

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
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("[JWT] Token validated.");
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
