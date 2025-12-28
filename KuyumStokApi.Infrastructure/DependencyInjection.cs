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
using KuyumStokApi.Infrastructure.Services.ThermalPrintersService;
using KuyumStokApi.Infrastructure.Services.ReportsService;
using KuyumStokApi.Infrastructure.Services.DashboardService;
using KuyumStokApi.Infrastructure.Services.AnomalyDetectionService;
using KuyumStokApi.Infrastructure.Services.WorkloadEstimationService;
using KuyumStokApi.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using KuyumStokApi.Infrastructure.QrCode;

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

            services.AddOptions<QrCodeOptions>()
                    .Bind(configuration.GetSection("QrCode"))
                    .ValidateDataAnnotations()
                    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "QrCode BaseUrl boş olamaz.")
                    .ValidateOnStart();


            services.AddSingleton<IJwtService, JwtService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher.PasswordHasher>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRefreshTokenService, KuyumStokApi.Infrastructure.Services.RefreshTokenService.RefreshTokenService>();
            services.AddScoped<ITokenBlacklistService, KuyumStokApi.Infrastructure.Services.TokenBlacklistService.TokenBlacklistService>();
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
            services.AddScoped<IThermalPrintersService, ThermalPrintersService>();
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<AnomalyDetectionService>();
            services.AddScoped<WorkloadEstimationService>();
            
            // DashboardService için IHubContext<DashboardHub> inject et
            services.AddScoped<IDashboardService>(sp =>
            {
                var db = sp.GetRequiredService<KuyumStokApi.Persistence.Contexts.AppDbContext>();
                var currentUser = sp.GetRequiredService<KuyumStokApi.Application.Interfaces.Auth.ICurrentUserContext>();
                var reportsService = sp.GetRequiredService<IReportsService>();
                var anomalyDetectionService = sp.GetRequiredService<AnomalyDetectionService>();
                var workloadEstimationService = sp.GetRequiredService<WorkloadEstimationService>();
                var hubContext = sp.GetService<IHubContext<DashboardHub>>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<DashboardService>>();
                
                return new DashboardService(
                    db,
                    currentUser,
                    reportsService,
                    anomalyDetectionService,
                    workloadEstimationService,
                    hubContext,
                    logger);
            });

            // Background service'i kaydet
            services.AddHostedService<DashboardBackgroundService>();

            return services;
        }
    }
}
