using HomeDashboard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeDashboard.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}
