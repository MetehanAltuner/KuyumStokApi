using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Auth
{
    /// <summary>
    /// Mevcut kullanıcının kimlik bilgilerine erişim sağlar.
    /// JWT token'dan UserId, BranchId ve authentication durumunu okur.
    /// </summary>
    public interface ICurrentUserContext
    {
        bool IsAuthenticated { get; }
        int? UserId { get; }
        int? BranchId { get; }
        string? UserName { get; }
    }
}
