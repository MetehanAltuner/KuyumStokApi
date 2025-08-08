using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Infrastructure.Auth;
using KuyumStokApi.Infrastructure.Services.JwtService;
using KuyumStokApi.Infrastructure.Services.UserService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<JwtOptions>()
                    .Bind(configuration.GetSection("Jwt"))
                    .ValidateDataAnnotations()
                    .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Jwt Key boş olamaz.")
                    .ValidateOnStart();

            services.AddOptions<PasswordOptions>()
                    .Bind(configuration.GetSection("Password"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();


            services.AddSingleton<IJwtService, JwtService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher.PasswordHasher>();
            services.AddScoped<IUserService, UserService>();


            return services;
        }
    }
}
