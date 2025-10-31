using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using refresh_flow_mvp_reactandnet.Server.Data;
using refresh_flow_mvp_reactandnet.Server.Entities; // sesuaikan
using refresh_flow_mvp_reactandnet.Server.Services.Contracts;

namespace refresh_flow_mvp_reactandnet.Server.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<RefreshTokenService> _logger;

        public TimeSpan RefreshTtl { get; } = TimeSpan.FromDays(14);
        public string RefreshCookieName => "rt";

        public RefreshTokenService(AppDbContext db, IHttpContextAccessor http, ILogger<RefreshTokenService> logger)
        {
            _db = db;
            _http = http;
            _logger = logger;
        }

        public async Task<(string plain, RefreshToken saved)> CreateAsync(string userId, string? device, string? ip, string? userAgent, string familyId)
        {
            var (plain, hash) = GenerateRefreshTokenPair();
            var now = DateTimeOffset.UtcNow;
            var record = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = now.Add(RefreshTtl),
                CreatedByIp = ip,
                UserAgent = userAgent,
                Device = device ?? "Unknown",
                Revoked = false,
                FamilyId = familyId
            };
            _db.RefreshTokens.Add(record);
            await _db.SaveChangesAsync();
            return (plain, record);
        }

        public async Task<RefreshToken?> FindByIncomingCookieAsync()
        {
            var token = _http.HttpContext?.Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(token)) return null;

            var hash = HashToken(token);
            return await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        }

        public async Task<(string newPlain, RefreshToken newToken)> RotateAsync(RefreshToken current, string? ip, string? userAgent)
        {
            var (plain, hash) = GenerateRefreshTokenPair();
            var now = DateTimeOffset.UtcNow;

            var next = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = current.UserId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = now.Add(RefreshTtl),
                CreatedByIp = ip,
                UserAgent = userAgent,
                Device = current.Device,
                Revoked = false,
                FamilyId = current.FamilyId
            };

            current.Revoked = true;
            current.RevokedAt = now;
            current.RevokedByIp = ip;
            current.ReplacedById = next.Id;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.RefreshTokens.Update(current);
                _db.RefreshTokens.Add(next);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return (plain, next);
        }

        public async Task RevokeAsync(RefreshToken token, string? ip)
        {
            if (token.Revoked) return;
            token.Revoked = true;
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ip;
            await _db.SaveChangesAsync();
        }

        public async Task<int> RevokeFamilyAsync(string userId, string familyId, string? reason, string? ip)
        {
            var tokens = await _db.RefreshTokens
                .Where(x => x.UserId == userId && x.FamilyId == familyId && !x.Revoked)
                .ToListAsync();

            foreach (var t in tokens)
            {
                t.Revoked = true;
                t.RevokedAt = DateTimeOffset.UtcNow;
                t.RevokedByIp = ip;
            }
            await _db.SaveChangesAsync();

            _logger.LogWarning("Revoked {Count} tokens for user {UserId} family {FamilyId}. Reason: {Reason}",
                tokens.Count, userId, familyId, reason);

            return tokens.Count;
        }

        public void SetCookie(string plain, DateTimeOffset expiresAt)
        {
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt.UtcDateTime,
                Path = "/auth/refresh"
            };
            _http.HttpContext!.Response.Cookies.Append(RefreshCookieName, plain, opts);
        }

        public void ClearCookie()
        {
            _http.HttpContext!.Response.Cookies.Append(RefreshCookieName, "",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1).UtcDateTime,
                    Path = "/auth/refresh"
                });
        }

        // ------- internal helpers -------
        private static (string plain, string hash) GenerateRefreshTokenPair()
        {
            var bytes = RandomNumberGenerator.GetBytes(32); // 256-bit
            var plain = Convert.ToBase64String(bytes);
            var hash = HashToken(plain);
            return (plain, hash);
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }
    }
}
