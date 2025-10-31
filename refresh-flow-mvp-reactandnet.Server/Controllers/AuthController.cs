using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using refresh_flow_mvp_reactandnet.Server.Data;
using refresh_flow_mvp_reactandnet.Server.Services.Contracts;

namespace refresh_flow_mvp_reactandnet.Server.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _users;
        private readonly SignInManager<AppUser> _signIn;
        private readonly IJwtTokenService _jwt;
        private readonly IRefreshTokenService _refresh;
        private readonly ILogger<AuthController> _log;

        public AuthController(
            AppDbContext db,
            UserManager<AppUser> users,
            SignInManager<AppUser> signIn,
            IJwtTokenService jwt,
            IRefreshTokenService refresh,
            ILogger<AuthController> log)
        {
            _db = db;
            _users = users;
            _signIn = signIn;
            _jwt = jwt;
            _refresh = refresh;
            _log = log;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Username/password required" });

            var user = await _users.Users.FirstOrDefaultAsync(x => x.UserName == req.Username);
            if (user is null)
            {
                await Task.Delay(Random.Shared.Next(50, 150));
                return Unauthorized(new { error = "Invalid credentials" });
            }

            var signInRes = await _signIn.CheckPasswordSignInAsync(user, req.Password, true);
            if (!signInRes.Succeeded)
                return Unauthorized(new { error = "Invalid credentials" });

            var familyId = Guid.NewGuid().ToString();
            var (rtPlain, rtRecord) = await _refresh.CreateAsync(
                user.Id, req.Device, ClientIp(), UserAgent(), familyId);

            _refresh.SetCookie(rtPlain, rtRecord.ExpiresAt);
            var (access, exp) = await _jwt.GenerateAsync(user);

            return Ok(new AuthResponse
            {
                AccessToken = access,
                ExpiresInSeconds = (int)exp.TotalSeconds,
                User = new UserInfo { Id = user.Id, Username = user.UserName ?? "", Email = user.Email ?? "" }
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var token = await _refresh.FindByIncomingCookieAsync();
            if (token is null)
            {
                _refresh.ClearCookie();
                return Unauthorized(new { error = "Missing/invalid refresh token" });
            }

            if (token.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _log.LogInformation("Expired refresh token for {UserId}", token.UserId);
                _refresh.ClearCookie();
                return Unauthorized(new { error = "Refresh token expired" });
            }

            if (token.Revoked)
            {
                await _refresh.RevokeFamilyAsync(token.UserId, token.FamilyId, "Reuse detected", ClientIp());
                _refresh.ClearCookie();
                return Unauthorized(new { error = "Session compromised. Please login again." });
            }

            var (newPlain, newToken) = await _refresh.RotateAsync(token, ClientIp(), UserAgent());

            var user = await _users.FindByIdAsync(token.UserId);
            if (user is null)
            {
                _refresh.ClearCookie();
                return Unauthorized(new { error = "User no longer exists" });
            }

            _refresh.SetCookie(newPlain, newToken.ExpiresAt);
            var (access, exp) = await _jwt.GenerateAsync(user);

            return Ok(new AuthResponse
            {
                AccessToken = access,
                ExpiresInSeconds = (int)exp.TotalSeconds,
                User = new UserInfo { Id = user.Id, Username = user.UserName ?? "", Email = user.Email ?? "" }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = await _refresh.FindByIncomingCookieAsync();
            if (token is not null)
                await _refresh.RevokeAsync(token, ClientIp());

            _refresh.ClearCookie();
            return Ok(new { message = "Logged out" });
        }

        [Authorize]
        [HttpGet("/me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return Unauthorized();

            return Ok(new UserInfo { Id = user.Id, Username = user.UserName ?? "", Email = user.Email ?? "" });
        }

        private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
        private string? UserAgent() => Request.Headers.UserAgent.ToString();
    }

    public class LoginRequest
    {
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? Device { get; set; }
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public int ExpiresInSeconds { get; set; }
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public string Id { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
    }

    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Key { get; set; } = default!;
    }
}
