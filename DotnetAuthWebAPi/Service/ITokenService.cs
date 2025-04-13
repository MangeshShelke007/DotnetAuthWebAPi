using DotnetAuthWebAPi.Domain.Entities;
using Microsoft.Extensions.Primitives;

namespace DotnetAuthWebAPi.Service
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser user);
        string GenerateRefreshToken();
    }
}
