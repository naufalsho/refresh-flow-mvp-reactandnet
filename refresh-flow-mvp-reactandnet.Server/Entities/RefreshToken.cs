using System.ComponentModel.DataAnnotations;

namespace refresh_flow_mvp_reactandnet.Server.Entities
{
    public class RefreshToken
    {
        [Key] public Guid Id { get; set; }
        [Required] public string UserId { get; set; } = default!;
        [Required] public string TokenHash { get; set; } = default!;   // hash server-side
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? CreatedByIp { get; set; }
        public bool Revoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public Guid? ReplacedById { get; set; }       // rotation link
        public string? UserAgent { get; set; }
        public string? Device { get; set; }
        public string FamilyId { get; set; } = Guid.NewGuid().ToString(); // untuk revoke keluarga
    }
}
