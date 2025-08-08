using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IPasswordHasher
    {
        string GenerateSalt(int size = 16); // Base64
        string Hash(string password, string saltBase64); // Base64 hash
        bool Verify(string password, string saltBase64, string expectedHashBase64);
    }
}
