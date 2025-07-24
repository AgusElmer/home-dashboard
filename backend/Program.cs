// Import necessary namespaces for authentication, data handling, and web functionalities.
using HomeDashboard.Api.Endpoints;
using HomeDashboard.Api.Extensions;
using static HomeDashboard.Api.Constants;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
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

// Initialize the web application builder.
var builder = WebApplication.CreateBuilder(args);
var FrontUrl = builder.Configuration["FrontUrl"] ?? throw new Exception("FrontUrl missing");

// Configure the database context to use SQLite with a connection string from configuration.
builder.Services.AddApplicationDbContext(builder.Configuration);

// Configure authentication services.
builder.Services.AddAppAuthentication(builder.Configuration);

// Add authorization services.
builder.Services.AddAuthorization();

// Configure Cross-Origin Resource Sharing (CORS) for development.
builder.Services.AddAppCors(builder.Configuration);

// Build the application.
var app = builder.Build();

// TODO: In production, database migrations should be applied as a separate step in your CI/CD pipeline.
// For development, you can uncomment the following block to apply migrations on startup.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


// Enable the CORS policy.
app.UseSecurityHeaders();
app.UseCors();

// Serve static files from the wwwroot folder
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable authentication and authorization middleware.
app.UseAuthentication();
app.UseAuthorization();

// Custom middleware for Cross-Site Request Forgery (CSRF) protection.
// Register the CSRF middleware.
app.UseCustomCsrfMiddleware();

// Define the application's API endpoints.
app.MapAuthEndpoints();
app.MapNoteEndpoints();

// Fallback for SPAs: always serve index.html for non-API routes
app.MapFallbackToFile("index.html");

// Run the application.
app.Run();