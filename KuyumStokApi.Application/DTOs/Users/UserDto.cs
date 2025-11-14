using System;

namespace KuyumStokApi.Application.DTOs.Users
{
    /// <summary>Kullanıcı detay modeli.</summary>
    public sealed class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public RoleBrief? Role { get; set; }
        public BranchBrief? Branch { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public sealed class RoleBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
        }

        public sealed class BranchBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
            public int? StoreId { get; set; }
            public string? StoreName { get; set; }
        }
    }

    /// <summary>Kullanıcı güncelleme modeli.</summary>
    public sealed class UserUpdateDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? RoleId { get; set; }
        public int? BranchId { get; set; }
        /// <summary>Boş bırakılırsa aktiflik değişmez.</summary>
        public bool? IsActive { get; set; }
        /// <summary>Dolu gönderilirse parola güncellenir.</summary>
        public string? Password { get; set; }
    }

    /// <summary>Kullanıcı listesi için filtre.</summary>
    public sealed record UserFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,
        int? RoleId = null,
        int? BranchId = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}

