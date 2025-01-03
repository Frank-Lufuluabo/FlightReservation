using System.Security.Claims;

namespace Shared.Authentication
{
    public interface TokenService
    {
        bool ValidateToken(string token);   
        IEnumerable<Claim> GetClaims(string token);
    }
}
