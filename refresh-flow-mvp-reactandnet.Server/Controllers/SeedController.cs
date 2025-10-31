using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using refresh_flow_mvp_reactandnet.Server.Data;

namespace refresh_flow_mvp_reactandnet.Server.Controllers
{
    public class DevSeedOptions
    {
        public bool Enabled { get; set; } = false;
        public string? Secret { get; set; }
    }

    [ApiController]
    [Route("dev/seed")]
    public class SeedController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DevSeedOptions _opt;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly UserManager<AppUser> _users;
        private readonly AppDbContext _db;
        private readonly ILogger<SeedController> _log;

        public SeedController(
            IWebHostEnvironment env,
            IOptions<DevSeedOptions> opt,
            RoleManager<IdentityRole> roles,
            UserManager<AppUser> users,
            AppDbContext db,
            ILogger<SeedController> log)
        {
            _env = env;
            _opt = opt.Value;
            _roles = roles;
            _users = users;
            _db = db;
            _log = log;
        }

        // POST /dev/seed
        // Header wajib: X-Seed-Secret: <secret>
        [HttpPost]
        public async Task<IActionResult> Run()
        {
            if (!_env.IsDevelopment() || !_opt.Enabled)
                return Forbid("Seeding disabled outside Development.");

            if (!Request.Headers.TryGetValue("X-Seed-Secret", out var provided) ||
                string.IsNullOrWhiteSpace(_opt.Secret) ||
                !CryptographicEquals(provided!, _opt.Secret!))
            {
                return Unauthorized(new { error = "Missing or invalid X-Seed-Secret" });
            }

            // pastikan DB siap
            await _db.Database.MigrateAsync();

            // 1) Roles
            var wantedRoles = new[] { "Admin", "User" };
            int roleCreated = 0;
            foreach (var r in wantedRoles)
            {
                if (!await _roles.RoleExistsAsync(r))
                {
                    var rr = await _roles.CreateAsync(new IdentityRole(r));
                    if (rr.Succeeded) roleCreated++;
                    else return Problem($"Create role {r} failed: {string.Join(",", rr.Errors.Select(e => e.Description))}");
                }
            }

            // 2) Users
            var created = new List<object>();

            async Task SeedUser(string username, string email, string password, params string[] roles)
            {
                var u = await _users.FindByNameAsync(username);
                if (u is null)
                {
                    u = new AppUser { UserName = username, Email = email, EmailConfirmed = true };
                    var res = await _users.CreateAsync(u, password);
                    if (!res.Succeeded)
                        throw new InvalidOperationException($"Create user {username} failed: {string.Join(",", res.Errors.Select(e => e.Description))}");

                    if (roles.Length > 0)
                    {
                        var rr = await _users.AddToRolesAsync(u, roles);
                        if (!rr.Succeeded)
                            throw new InvalidOperationException($"Add roles to {username} failed: {string.Join(",", rr.Errors.Select(e => e.Description))}");
                    }
                    created.Add(new { username, email });
                }
            }

            // Password dev: aman tapi mudah diingat — ganti kalau perlu
            await SeedUser("admin", "admin@example.com", "Passw0rd!", "Admin");
            await SeedUser("demo", "demo@example.com", "Passw0rd!", "User");

            // 3) (Opsional) seed data tambahan ringan untuk demo RefreshTokens?
            // Tidak membuat refresh token di seed agar alur login diuji end-to-end.

            _log.LogInformation("Dev seed done. rolesCreated={roleCreated} usersCreated={usersCreated}", roleCreated, created.Count);

            return Ok(new
            {
                message = "Seeding completed",
                rolesCreated = roleCreated,
                usersCreated = created.Count,
                users = created
            });
        }

        // constant-time compare
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
