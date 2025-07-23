// Import necessary namespaces for authentication, data handling, and web functionalities.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HomeDashboard.Api.Data;
using HomeDashboard.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Define constants for authentication schemes, cookie names, and the frontend URL.
const string JwtScheme    = JwtBearerDefaults.AuthenticationScheme;
const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
const string AuthCookie   = "auth_token";     // HttpOnly cookie with JWT
const string CsrfCookie   = "XSRF-TOKEN";     // Non-HttpOnly cookie with CSRF token
const string CsrfHeader   = "X-XSRF-TOKEN";   // Header the client must send back
const string FrontUrl     = "http://localhost:5173";

// Initialize the web application builder.
var builder = WebApplication.CreateBuilder(args);

// Configure the database context to use SQLite with a connection string from configuration.
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Configure authentication services.
builder.Services.AddAuthentication(options =>
{
    // Set the default schemes for authentication, challenge, and sign-in.
    options.DefaultAuthenticateScheme = JwtScheme;
    options.DefaultChallengeScheme    = JwtScheme;
    options.DefaultSignInScheme       = CookieScheme;
})
// Add cookie-based authentication, used temporarily for the Google OAuth flow.
.AddCookie(CookieScheme, o =>
{
    // This cookie is not for persistent sessions, just to complete the Google auth handshake.
    o.Cookie.SameSite = SameSiteMode.Lax;
})
// Add JWT bearer authentication for securing API endpoints.
.AddJwtBearer(options =>
{
    // Retrieve the JWT secret key from configuration.
    var key = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key missing");
    // Configure token validation parameters.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };

    // Define an event to read the JWT from the HttpOnly cookie if it's not in the Authorization header.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (string.IsNullOrEmpty(ctx.Token))
            {
                ctx.Request.Cookies.TryGetValue(AuthCookie, out var token);
                ctx.Token = token;
            }
            return Task.CompletedTask;
        }
    };
})
// Add Google as an external authentication provider.
.AddGoogle("Google", options =>
{
    // Configure Google client ID, client secret, and callback path from configuration.
    options.ClientId     = builder.Configuration["Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
    options.CallbackPath = "/auth/google-callback";
    options.SignInScheme = CookieScheme; // Use the cookie scheme to temporarily sign in the user.
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
});

// Add authorization services.
builder.Services.AddAuthorization();

// Configure Cross-Origin Resource Sharing (CORS) for development.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(FrontUrl) // Allow requests from the frontend development server.
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // Allow credentials (cookies) to be sent.
});

// Build the application.
var app = builder.Build();

// Automatically apply database migrations on startup (for development environments).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Enable the CORS policy.
app.UseCors();

// Serve static files from the wwwroot folder
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable authentication and authorization middleware.
app.UseAuthentication();
app.UseAuthorization();

// Custom middleware for Cross-Site Request Forgery (CSRF) protection.
bool CheckCsrf(HttpContext ctx)
{
    // GET requests are safe and don't need CSRF protection.
    if (HttpMethods.IsGet(ctx.Request.Method)) return true;
    // For other methods, validate the CSRF token from the cookie against the header.
    if (!ctx.Request.Cookies.TryGetValue(CsrfCookie, out var cookieVal)) return false;
    if (!ctx.Request.Headers.TryGetValue(CsrfHeader, out var headerVal)) return false;
    return cookieVal == headerVal;
}

// Register the CSRF middleware.
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

// Define the application's API endpoints.

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

// A protected API endpoint to get the notes for the current user.
app.MapGet("/api/notes", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var email = user.FindFirst(ClaimTypes.Email)!.Value;
    return await db.Notes.Where(n => n.OwnerEmail == email).OrderByDescending(n => n.CreatedAt).ToListAsync();
}).RequireAuthorization();

// A protected API endpoint to create a new note.
app.MapPost("/api/notes", async (ClaimsPrincipal user, AppDbContext db, Note note) =>
{
    // Set server-side properties for the new note.
    note.Id         = 0; // Let the database generate the ID.
    note.OwnerEmail = user.FindFirst(ClaimTypes.Email)!.Value;
    note.CreatedAt  = DateTime.UtcNow;
    // Add the note to the database and save changes.
    db.Notes.Add(note);
    await db.SaveChangesAsync();
    // Return a 201 Created response with the new note.
    return Results.Created($"/api/notes/{note.Id}", note);
})
.RequireAuthorization();

// Fallback for SPAs: always serve index.html for non-API routes
app.MapFallbackToFile("index.html");

// Run the application.
app.Run();