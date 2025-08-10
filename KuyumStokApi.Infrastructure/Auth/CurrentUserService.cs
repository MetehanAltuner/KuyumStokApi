using KuyumStokApi.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace KuyumStokApi.Infrastructure.Auth
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http;
        public CurrentUserService(IHttpContextAccessor http) => _http = http;

        public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? UserName
        {
            get
            {
                var u = _http.HttpContext?.User;
                return u?.Identity?.Name
                    ?? u?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                    ?? u?.FindFirst(ClaimTypes.Name)?.Value;
            }
        }

        public int? UserId
        {
            get
            {
                var u = _http.HttpContext?.User;
                var id =
                    u?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    u?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return int.TryParse(id, out var i) ? i : null;
            }
        }
    }

}
