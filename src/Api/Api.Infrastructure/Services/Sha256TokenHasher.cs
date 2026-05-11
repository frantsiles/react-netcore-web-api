using Api.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Api.Infrastructure.Services;

public class Sha256TokenHasher : ITokenHasher
{
    public string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
