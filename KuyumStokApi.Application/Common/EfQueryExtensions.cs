using KuyumStokApi.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Common
{
    public static class EfQueryExtensions
    {
        public static IQueryable<T> WithDeleted<T>(this IQueryable<T> q) where T : class
            => q.IgnoreQueryFilters();

        /// Sadece aktif olanları getir
        public static IQueryable<T> OnlyActive<T>(this IQueryable<T> q) where T : class, IActivatable
            => q.Where(x => x.IsActive);
    }
}
