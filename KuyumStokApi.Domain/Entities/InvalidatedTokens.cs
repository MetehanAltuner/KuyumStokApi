using System;

namespace KuyumStokApi.Domain.Entities;

public partial class InvalidatedTokens
{
    public int Id { get; set; }

    public string Jti { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime InvalidatedAt { get; set; }
}

