using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Roles
{
    public sealed class RoleDto
    {
        /// <summary>Rol Id</summary>
        public int Id { get; set; }

        /// <summary>Rol adı</summary>
        public string Name { get; set; } = null!;

        /// <summary>Oluşturulma zamanı (UTC)</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Güncellenme zamanı (UTC)</summary>
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class RoleCreateDto
    {
        /// <summary>Rol adı (zorunlu)</summary>
        public string Name { get; set; } = null!;
    }

    public sealed class RoleUpdateDto
    {
        /// <summary>Rol adı (zorunlu)</summary>
        public string Name { get; set; } = null!;
    }
}
