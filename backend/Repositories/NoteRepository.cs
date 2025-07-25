using HomeDashboard.Api.Data;
using HomeDashboard.Api.Models;

namespace HomeDashboard.Api.Repositories
{
    public class NoteRepository : Repository<Note>, INoteRepository
    {
        public NoteRepository(AppDbContext context) : base(context)
        {
        }
    }
}