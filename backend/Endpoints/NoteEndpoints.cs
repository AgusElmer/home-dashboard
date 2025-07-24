using System.Security.Claims;
using HomeDashboard.Api.Data;
using HomeDashboard.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HomeDashboard.Api.Endpoints
{
    public static class NoteEndpoints
    {
        public static void MapNoteEndpoints(this IEndpointRouteBuilder app)
        {
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
        }
    }
}
