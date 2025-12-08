using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KuyumStokApi.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Hubs
{
    /// <summary>
    /// Dashboard için real-time güncellemeler için SignalR hub
    /// </summary>
    [Authorize]
    public class DashboardHub : Hub
    {
        private readonly ILogger<DashboardHub> _logger;

        public DashboardHub(ILogger<DashboardHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Client bağlandığında bağlantıyı kabul eder ve ilk verileri gönderir
        /// Sonrasında background service periyodik olarak güncellemeleri gönderecek
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            
            _logger.LogInformation("Client connected to DashboardHub. ConnectionId: {ConnectionId}, UserId: {UserId}, UserName: {UserName}", 
                Context.ConnectionId, userId, userName);

            await base.OnConnectedAsync();

            // İlk bağlantıda hemen veri gönder (paralel olarak, her biri bağımsız)
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext != null)
                {
                    // Hub context'inde IHttpContextAccessor'ın doğru HTTP context'i göstermesi için
                    // HTTP context'i manuel olarak set et
                    var httpContextAccessor = httpContext.RequestServices.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                    if (httpContextAccessor != null && httpContextAccessor.HttpContext == null)
                    {
                        // Reflection ile HttpContextAccessor'ın internal field'ını set et
                        var field = typeof(Microsoft.AspNetCore.Http.HttpContextAccessor).GetField("_httpContext", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            // HttpContextHolder oluştur ve set et
                            var holderType = typeof(Microsoft.AspNetCore.Http.HttpContextAccessor).GetNestedType("HttpContextHolder", 
                                System.Reflection.BindingFlags.NonPublic);
                            if (holderType != null)
                            {
                                var holder = Activator.CreateInstance(holderType);
                                var contextField = holderType.GetField("Context", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if (contextField != null)
                                {
                                    contextField.SetValue(holder, httpContext);
                                    field.SetValue(httpContextAccessor, holder);
                                }
                            }
                        }
                    }
                    
                    var serviceProvider = httpContext.RequestServices;
                    var dashboardService = serviceProvider.GetRequiredService<IDashboardService>();
                    
                    // Tüm event'leri paralel olarak gönder, birisi hata verse bile diğerleri gönderilsin
                    var tasks = new List<Task>
                    {
                        // Summary gönder (tüm verileri içerir)
                        SendInitialDataAsync(dashboardService, "SummaryUpdated", async () =>
                        {
                            try
                            {
                                var result = await dashboardService.GetSummaryAsync();
                                if (result.Success && result.Data != null)
                                {
                                    await Clients.Caller.SendAsync("SummaryUpdated", result.Data);
                                    _logger.LogInformation("SummaryUpdated sent to client {ConnectionId}, Data: {HasData}", 
                                        Context.ConnectionId, result.Data != null);
                                    return true;
                                }
                                else
                                {
                                    _logger.LogWarning("GetSummaryAsync failed for client {ConnectionId}: Success={Success}, Message={Message}", 
                                        Context.ConnectionId, result.Success, result.Message);
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Exception in GetSummaryAsync for client {ConnectionId}", Context.ConnectionId);
                                return false;
                            }
                        }),

                        // LiveCounters gönder
                        SendInitialDataAsync(dashboardService, "LiveCountersUpdated", async () =>
                        {
                            var result = await dashboardService.GetLiveCountersAsync();
                            if (result.Success && result.Data != null)
                            {
                                await Clients.Caller.SendAsync("LiveCountersUpdated", result.Data);
                                return true;
                            }
                            return false;
                        }),

                        // DailySummary gönder
                        SendInitialDataAsync(dashboardService, "DailySummaryUpdated", async () =>
                        {
                            var result = await dashboardService.GetDailySummaryAsync(null);
                            if (result.Success && result.Data != null)
                            {
                                await Clients.Caller.SendAsync("DailySummaryUpdated", result.Data);
                                return true;
                            }
                            return false;
                        }),

                        // Anomalies gönder
                        SendInitialDataAsync(dashboardService, "AnomaliesUpdated", async () =>
                        {
                            var result = await dashboardService.GetAnomaliesAsync();
                            if (result.Success && result.Data != null)
                            {
                                await Clients.Caller.SendAsync("AnomaliesUpdated", result.Data);
                                return true;
                            }
                            return false;
                        })
                    };

                    // Tüm task'ları paralel olarak çalıştır, hata olsa bile diğerleri devam etsin
                    await Task.WhenAll(tasks);
                }
            }
        }

        /// <summary>
        /// Client bağlantısı kesildiğinde loglama yapar
        /// </summary>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client disconnected from DashboardHub with error. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Client disconnected from DashboardHub. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// İlk bağlantıda veri göndermek için helper metod
        /// Her event için ayrı try-catch ile hata yönetimi yapar
        /// </summary>
        private async Task SendInitialDataAsync(IDashboardService dashboardService, string eventName, Func<Task<bool>> sendAction)
        {
            try
            {
                var success = await sendAction();
                if (success)
                {
                    _logger.LogInformation("Initial {EventName} sent to client {ConnectionId}", eventName, Context.ConnectionId);
                }
                else
                {
                    _logger.LogWarning("Failed to send initial {EventName} to client {ConnectionId}", eventName, Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending initial {EventName} to client {ConnectionId}", eventName, Context.ConnectionId);
                // Hata olsa bile diğer event'ler gönderilmeye devam edecek
            }
        }

        /// <summary>
        /// Client manuel olarak summary güncellemesi isteyebilir
        /// Not: Bu metod authentication gerektirir ve kullanıcı bilgilerine ihtiyaç duyar
        /// </summary>
        public async Task RequestSummary()
        {
            try
            {
                // Authentication kontrolü
                if (Context.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogWarning("RequestSummary called without authentication. ConnectionId: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("Error", "Authentication required");
                    return;
                }

                var serviceProvider = Context.GetHttpContext()?.RequestServices;
                if (serviceProvider != null)
                {
                    var dashboardService = serviceProvider.GetRequiredService<IDashboardService>();
                    var result = await dashboardService.GetSummaryAsync();
                    if (result.Success && result.Data != null)
                    {
                        await Clients.Caller.SendAsync("SummaryUpdated", result.Data);
                        _logger.LogInformation("Summary sent to client {ConnectionId}", Context.ConnectionId);
                    }
                    else
                    {
                        _logger.LogWarning("Summary request failed for client {ConnectionId}: {Message}", 
                            Context.ConnectionId, result.Message);
                        await Clients.Caller.SendAsync("Error", result.Message ?? "Summary alınamadı");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling RequestSummary from client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Summary istenirken hata oluştu");
            }
        }
    }
}

