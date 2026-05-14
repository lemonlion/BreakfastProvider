namespace BreakfastProvider.Api.Models.Requests;

public record ChefNoteRequest
{
    public string? RecipeName { get; init; }
    public string? ChefName { get; init; }
    public string? NoteText { get; init; }
    public string? Category { get; init; }
}
