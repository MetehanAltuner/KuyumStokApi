using KuyumStokApi.Infrastructure;
using KuyumStokApi.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

builder.Services.AddControllers();

builder.Services.AddPersistence(cfg);
builder.Services.AddInfrastructure(cfg);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Key'i Base64 ürettik → byte[] olarak çöz
        var keyBytes = Convert.FromBase64String(cfg["Jwt:Key"]!);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = cfg["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // mümkünse sıfır
        };

        options.RequireHttpsMetadata = true; // dev ortamda http ise false yapabilirsin
        options.SaveToken = false;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KuyumStokApi", Version = "v1" });
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token}"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
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
