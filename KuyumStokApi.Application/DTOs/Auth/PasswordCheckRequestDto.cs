using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Auth
{
        public sealed class PasswordCheckRequestDto
        {
            public string Password { get; set; } = default!;
            public string? Username { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
        }

        public sealed class PasswordCheckResultDto
        {
            public bool IsValid { get; set; }
            public int Score { get; set; }
            public List<string> Errors { get; set; } = new();
        }

        public sealed class RegisterValidationResultDto
        {
            public bool IsValid { get; set; }
            public int PasswordScore { get; set; }
            public List<string> Errors { get; set; } = new();
        }
    }
