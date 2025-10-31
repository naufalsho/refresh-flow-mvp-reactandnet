using refresh_flow_mvp_reactandnet.Server.Data;
using refresh_flow_mvp_reactandnet.Server.Entities; // kalau RefreshToken Anda di namespace ini, sesuaikan

namespace refresh_flow_mvp_reactandnet.Server.Services.Contracts
{
    public interface IRefreshTokenService
    {
        // Membuat token baru (plain + record DB) dalam sebuah family (baru/lama)
        Task<(string plain, RefreshToken saved)> CreateAsync(string userId, string? device, string? ip, string? userAgent, string familyId);

        // Validasi dari cookie, return record (bisa revoked/expired) atau null
        Task<RefreshToken?> FindByIncomingCookieAsync();

        // Rotasi: revoke lama, buat baru dalam 1 transaksi
        Task<(string newPlain, RefreshToken newToken)> RotateAsync(RefreshToken current, string? ip, string? userAgent);

        // Revoke satu token
        Task RevokeAsync(RefreshToken token, string? ip);

        // Revoke seluruh family
        Task<int> RevokeFamilyAsync(string userId, string familyId, string? reason, string? ip);

        // Cookie ops
        void SetCookie(string plain, DateTimeOffset expiresAt);
        void ClearCookie();

        // Konstanta
        TimeSpan RefreshTtl { get; }
        string RefreshCookieName { get; }
    }
}
