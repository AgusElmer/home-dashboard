namespace HomeDashboard.Api.Models;

public class Note
{
    public int Id { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string OwnerEmail { get; set; } = "";
}