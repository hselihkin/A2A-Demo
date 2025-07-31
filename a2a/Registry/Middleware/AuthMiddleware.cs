namespace Registry.Middleware
{
    using Microsoft.AspNetCore.Http;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Registry.Data;

    public class ApiKeyMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context, ApplicationDbContext dbContext)
        {
            var path = context.Request.Path.Value;
            Console.WriteLine(path);
            if (path != null && (path.StartsWith("/signup") || path.Contains("agent")))
            {
                await _next(context);
                return;
            }
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing or invalid Authorization header.");
                return;
            }

            var token = authHeader["Bearer ".Length..].Trim();

            var validKey = await dbContext.Tokens
                .Where(k => k.K == token && k.Active)
                .FirstOrDefaultAsync();

            if (validKey == null)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Unauthorized: Invalid API key.");
                return;
            }

            context.Items["ApiKeyOwner"] = validKey.ServerName;

            await _next(context);
        }
    }

}
