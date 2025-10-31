using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using refresh_flow_mvp_reactandnet.Server.Controllers;
using refresh_flow_mvp_reactandnet.Server.Data;
using refresh_flow_mvp_reactandnet.Server.Services.Contracts;

namespace refresh_flow_mvp_reactandnet.Server.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly JwtOptions _opt;
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

        public JwtTokenService(UserManager<AppUser> userManager, IOptions<JwtOptions> opt)
        {
            _userManager = userManager;
            _opt = opt.Value;
        }

        public async Task<(string token, TimeSpan expiresIn)> GenerateAsync(AppUser user, TimeSpan? lifetimeOverride = null)
        {
            var ttl = lifetimeOverride ?? DefaultTtl;
            var now = DateTimeOffset.UtcNow;

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: now.Add(ttl).UtcDateTime,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(jwt), ttl);
        }
    }
}
