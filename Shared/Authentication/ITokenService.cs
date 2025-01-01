using System.Security.Claims;

namespace Shared.Authentication
{
    public interface ITokenService
    {
        bool ValidateToken(string token);   
        IEnumerable<Claim> GetClaims(string token);
    }
}
