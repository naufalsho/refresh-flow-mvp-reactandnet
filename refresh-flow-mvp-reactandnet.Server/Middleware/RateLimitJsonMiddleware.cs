namespace refresh_flow_mvp_reactandnet.Server.Middleware
{
    public class RateLimitJsonMiddleware
    {
        private readonly RequestDelegate _next;
        public RateLimitJsonMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests &&
                context.Response.ContentLength is null) // belum ada body
            {
                context.Response.ContentType = "application/json";
                var limit = context.Response.Headers["X-Rate-Limit-Limit"].FirstOrDefault()
                            ?? context.Response.Headers["X-RateLimit-Limit"].FirstOrDefault();
                var remaining = context.Response.Headers["X-Rate-Limit-Remaining"].FirstOrDefault()
                                ?? context.Response.Headers["X-RateLimit-Remaining"].FirstOrDefault();
                var reset = context.Response.Headers["X-Rate-Limit-Reset"].FirstOrDefault()
                            ?? context.Response.Headers["X-RateLimit-Reset"].FirstOrDefault();

                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "rate_limited",
                    message = "Too many requests. Please retry later.",
                    limit,
                    remaining,
                    reset
                });
                await context.Response.WriteAsync(payload);
            }
        }
    }

    public static class RateLimitJsonMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimitJson(this IApplicationBuilder app) =>
            app.UseMiddleware<RateLimitJsonMiddleware>();
    }
}
