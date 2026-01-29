using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace KuyumStokApi.Application.DTOs.Auth;

public class RegisterDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "RoleId must be greater than 0.")]
    public int? RoleId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
    public int? BranchId { get; set; }
}

