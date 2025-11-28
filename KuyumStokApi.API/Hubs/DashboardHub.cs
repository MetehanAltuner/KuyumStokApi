using Microsoft.AspNetCore.SignalR;

namespace KuyumStokApi.API.Hubs
{
    /// <summary>
    /// Dashboard için real-time güncellemeler için SignalR hub
    /// </summary>
    public class DashboardHub : Hub
    {
        // Hub metodları frontend'den çağrılabilir
        // Broadcast işlemleri DashboardService içinden yapılacak
    }
}

