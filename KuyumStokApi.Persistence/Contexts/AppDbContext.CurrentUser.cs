using KuyumStokApi.Application.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Persistence.Contexts
{
    public partial class AppDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUser;

        public AppDbContext(DbContextOptions<AppDbContext> options,
                            ICurrentUserService? currentUser) : base(options)
        {
            _currentUser = currentUser;
        }
    }
}
