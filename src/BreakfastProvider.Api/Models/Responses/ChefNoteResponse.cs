namespace BreakfastProvider.Api.Models.Responses;

public class ChefNoteResponse
{
    public string NoteId { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string ChefName { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
