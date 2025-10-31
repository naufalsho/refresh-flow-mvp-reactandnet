using System.Security.Claims;

namespace refresh_flow_mvp_reactandnet.Server.Middleware
{
    public class SecurityEventsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityEventsMiddleware> _logger;

        public SecurityEventsMiddleware(RequestDelegate next, ILogger<SecurityEventsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext ctx)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _next(ctx);
            sw.Stop();

            if (ctx.Request.Path.StartsWithSegments("/auth") || ctx.Response.StatusCode is 401 or 403 or 429)
            {
                var uid = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "-";
                _logger.LogInformation("SEC {Method} {Path} -> {Status} ({Ms} ms) uid={Uid} ip={Ip} ua={Ua}",
                    ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds,
                    uid,
                    ctx.Connection.RemoteIpAddress?.ToString(),
                    ctx.Request.Headers.UserAgent.ToString());
            }
        }
    }

    public static class SecurityEventsMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityEvents(this IApplicationBuilder app) =>
            app.UseMiddleware<SecurityEventsMiddleware>();
    }
}
