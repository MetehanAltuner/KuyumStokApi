using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Auth
{
    public sealed class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _http;
        public CurrentUserContext(IHttpContextAccessor http) => _http = http;

        public int? UserId => ReadInt("userId", "sub", ClaimTypes.NameIdentifier);
        public int? BranchId => ReadInt("branchId", "branch_id", "branch");

        private int? ReadInt(params string[] keys)
        {
            var claims = _http.HttpContext?.User?.Claims;
            if (claims is null) return null;

            foreach (var k in keys)
            {
                var v = claims.FirstOrDefault(c => c.Type.Equals(k, System.StringComparison.OrdinalIgnoreCase))?.Value;
                if (int.TryParse(v, out var num)) return num;
            }
            return null;
        }
    }
}
