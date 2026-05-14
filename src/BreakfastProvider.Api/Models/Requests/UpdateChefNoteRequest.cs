namespace BreakfastProvider.Api.Models.Requests;

public record UpdateChefNoteRequest
{
    public string? NoteText { get; init; }
    public string? Category { get; init; }
}
