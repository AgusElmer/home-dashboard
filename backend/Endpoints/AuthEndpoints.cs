using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HomeDashboard.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static HomeDashboard.Api.Constants;

namespace HomeDashboard.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            // The start of the Google OAuth 2.0 login flow.
            // This redirects the user to Google's login page.
            app.MapGet("/auth/login-google", () =>
                Results.Challenge(new AuthenticationProperties { RedirectUri = "/auth/google-complete" }, new[] { "Google" })
            );

            // The callback endpoint that Google redirects to after the user authenticates.
            app.MapGet("/auth/google-callback", () => Results.Redirect("/auth/google-complete"));

            // The final step of the login process.
            // This endpoint is called after a successful Google authentication.
            app.MapGet("/auth/google-complete", async (HttpContext ctx) =>
            {
                // Authenticate using the temporary cookie to get the user's claims.
                var result = await ctx.AuthenticateAsync(CookieScheme);
                if (!result.Succeeded) return Results.Unauthorized();

                // Extract the user's email from the claims.
                var email  = result.Principal!.FindFirst(ClaimTypes.Email)!.Value;
                var config = ctx.RequestServices.GetRequiredService<IConfiguration>();

                // Create a new JWT for the user.
                var key   = config["Jwt:Key"]!;
                var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
                var jwt   = new JwtSecurityToken(
                    issuer:   config["Jwt:Issuer"],
                    audience: config["Jwt:Audience"],
                    claims:   new[] { new Claim(ClaimTypes.Email, email) },
                    expires:  DateTime.UtcNow.AddHours(12),
                    signingCredentials: creds
                );
                var token = new JwtSecurityTokenHandler().WriteToken(jwt);

                // Set the JWT in a secure, HttpOnly cookie.
                ctx.Response.Cookies.Append(AuthCookie, token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddHours(12) });
                // Set the CSRF token in a separate, non-HttpOnly cookie that the client can read.
                var csrf  = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                ctx.Response.Cookies.Append(CsrfCookie, csrf, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddHours(12) });

                // Redirect the user back to the frontend application.
                var FrontUrl = config["FrontUrl"] ?? throw new Exception("FrontUrl missing");
                return Results.Redirect(FrontUrl);
            });

            // The logout endpoint.
            app.MapPost("/auth/logout", async (HttpContext ctx) =>
            {
                // Delete the authentication cookie.
                ctx.Response.Cookies.Delete(AuthCookie);

                // Sign out from the temporary cookie scheme as well.
                await ctx.SignOutAsync(CookieScheme);
                return Results.Ok();
            });

            // A protected API endpoint to get the current user's information.
            // Requires a valid JWT to access.
            app.MapGet("/api/me", (ClaimsPrincipal user) => new { email = user.FindFirst(ClaimTypes.Email)?.Value })
               .RequireAuthorization();
        }
    }
}
