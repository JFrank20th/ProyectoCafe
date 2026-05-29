using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProyectoCafe.API.Auth;

public interface IJwtTokenService
{
    string CreateToken(JwtUser user);
}

public sealed class JwtTokenService(IOptions<AuthOptions> options) : IJwtTokenService
{
    private readonly AuthOptions _options = options.Value;

    public string CreateToken(JwtUser user)
    {
        var jwt = _options.Jwt;

        var claims = new List<Claim>
        {
            new("uid", user.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Nombre),
            new("name", user.Nombre),
        };

        foreach (var role in user.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(jwt.AccessTokenLifetimeMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed record JwtUser(int UsuarioId, string Email, string Nombre, IReadOnlyCollection<string> Roles);
