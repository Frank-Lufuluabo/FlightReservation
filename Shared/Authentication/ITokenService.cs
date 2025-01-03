using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shared.Authentication
{
    public class ITokenService(IConfiguration config) : TokenService
    {
        public IEnumerable<Claim> GetClaims(string token)
        {
           var handler = new JwtSecurityTokenHandler();
            if(handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken?.Claims!.ToList()!;
            }
            return [];
        }

        public bool ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try 
            {
                var validateParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = config["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config[config["Audience"]!],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                   handler.ValidateToken(token, validateParameters, out SecurityToken validateToken);
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
