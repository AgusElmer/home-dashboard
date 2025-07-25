using System.Security.Claims;
using HomeDashboard.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using HomeDashboard.Api.Repositories;

namespace HomeDashboard.Api.Endpoints
{
    public static class NoteEndpoints
    {
        public static void MapNoteEndpoints(this IEndpointRouteBuilder app)
        {
            // A protected API endpoint to get the notes for the current user.
            app.MapGet("/api/notes", async (ClaimsPrincipal user, INoteRepository noteRepository) =>
            {
                var email = user.FindFirst(ClaimTypes.Email)!.Value;
                return await noteRepository.FindAsync(n => n.OwnerEmail == email);
            }).RequireAuthorization();

            // A protected API endpoint to create a new note.
            app.MapPost("/api/notes", async (ClaimsPrincipal user, INoteRepository noteRepository, Note note) =>
            {
                // Set server-side properties for the new note.
                note.Id         = 0; // Let the database generate the ID.
                note.OwnerEmail = user.FindFirst(ClaimTypes.Email)!.Value;
                note.CreatedAt  = DateTime.UtcNow;
                // Add the note to the database and save changes.
                await noteRepository.AddAsync(note);
                // Return a 201 Created response with the new note.
                return Results.Created($"/api/notes/{note.Id}", note);
            })
            .RequireAuthorization();
        }
    }
}
