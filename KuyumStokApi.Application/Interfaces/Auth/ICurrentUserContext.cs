using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Auth
{
    public interface ICurrentUserContext
    {
        int? UserId { get; }
        int? BranchId { get; }
    }
}
