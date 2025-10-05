using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Infrastructure.Auth;
using KuyumStokApi.Infrastructure.Services.JwtService;
using KuyumStokApi.Infrastructure.Services.ProductCategoryService;
using KuyumStokApi.Infrastructure.Services.UserService;
using KuyumStokApi.Infrastructure.Services.BanksService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KuyumStokApi.Application.Common;
using KuyumStokApi.Infrastructure.Services.ProductTypService;
using KuyumStokApi.Infrastructure.Services.ProductVariantService;
using KuyumStokApi.Infrastructure.Services.StocksService;
using KuyumStokApi.Infrastructure.Services.BranchesService;
using KuyumStokApi.Infrastructure.Services.CustomersService;
using KuyumStokApi.Infrastructure.Services.PaymentMethodsService;
using KuyumStokApi.Infrastructure.Services.SalesService;
using KuyumStokApi.Infrastructure.Services.RolesService;
using KuyumStokApi.Infrastructure.Services.LimitsService;
using KuyumStokApi.Infrastructure.Services.LifecycleActionsService;
using KuyumStokApi.Infrastructure.Services.ProductLifecycleService;
using KuyumStokApi.Infrastructure.Services.StoresService;
using KuyumStokApi.Infrastructure.Services.PurchasesService;

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
            services.AddScoped<IProductCategoryService, ProductCategoryService>();
            services.AddScoped<IBanksService, BanksService>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IProductTypeService, ProductTypeService>();
            services.AddScoped<IProductVariantService, ProductVariantService>();
            services.AddScoped<IStocksService, StocksService>();
            services.AddScoped<IBranchesService, BranchesService>();
            services.AddScoped<IStoresService, StoresService>();
            services.AddScoped<ICustomersService, CustomersService>();
            services.AddScoped<IPaymentMethodsService, PaymentMethodsService>();
            services.AddScoped<IPurchasesService, PurchasesService>();
            services.AddScoped<ISalesService, SalesService>();
            services.AddScoped<IRolesService, RolesService>();
            services.AddScoped<ILimitsService, LimitsService>();
            services.AddScoped<ILifecycleActionsService, LifecycleActionsService>();
            services.AddScoped<IProductLifecyclesService, ProductLifecyclesService>();

            return services;
        }
    }
}
