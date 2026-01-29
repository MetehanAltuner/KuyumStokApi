using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>
    /// Dashboard verilerindeki değişiklikler için SignalR broadcast yönetimi
    /// </summary>
    public interface IDashboardNotificationService
    {
        /// <summary>
        /// Entity değişikliklerine göre ilgili dashboard broadcast'lerini tetikler
        /// </summary>
        Task NotifyDashboardChangesAsync(
            IEnumerable<string> changedEntityTypes, 
            CancellationToken ct = default);

        /// <summary>
        /// Satış commit sonrası dashboard broadcast'lerini tetikler
        /// </summary>
        Task NotifySaleCommittedAsync(
            int? saleId,
            int? purchaseId,
            CancellationToken ct = default);
    }
}
