using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using static HomeDashboard.Api.Constants;

namespace HomeDashboard.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomCsrfMiddleware(this IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                // Apply CSRF check only for state-changing HTTP methods.
                if (HttpMethods.IsPost(ctx.Request.Method) ||
                    HttpMethods.IsPut(ctx.Request.Method)  ||
                    HttpMethods.IsDelete(ctx.Request.Method) ||
                    HttpMethods.IsPatch(ctx.Request.Method))
                {
                    if (!CheckCsrf(ctx))
                    {
                        // If validation fails, return a 403 Forbidden response.
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await ctx.Response.WriteAsJsonAsync(new { title = "CSRF validation failed", status = 403 });
                        return;
                    }
                }
                // If validation succeeds, proceed to the next middleware.
                await next();
            });
            return app;
        }

        private static bool CheckCsrf(HttpContext ctx)
        {
            // GET requests are safe and don't need CSRF protection.
            if (HttpMethods.IsGet(ctx.Request.Method)) return true;
            // For other methods, validate the CSRF token from the cookie against the header.
            if (!ctx.Request.Cookies.TryGetValue(CsrfCookie, out var cookieVal)) return false;
            if (!ctx.Request.Headers.TryGetValue(CsrfHeader, out var headerVal)) return false;
            return cookieVal == headerVal;
        }

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
            return app;
        }
    }
}