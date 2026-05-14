using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BreakfastProvider.Api.Services;

public interface IChefNoteService
{
    Task<ChefNoteResponse> CreateAsync(ChefNoteRequest request, CancellationToken cancellationToken = default);
    Task<ChefNoteResponse?> GetByIdAsync(string noteId, CancellationToken cancellationToken = default);
    Task<ChefNoteResponse?> UpdateAsync(string noteId, UpdateChefNoteRequest request, CancellationToken cancellationToken = default);
    Task<List<ChefNoteResponse>> ListByRecipeAsync(string recipeName, CancellationToken cancellationToken = default);
}

public class ChefNoteService(IMongoClient mongoClient, ILogger<ChefNoteService> logger) : IChefNoteService
{
    private IMongoCollection<ChefNoteDocument> Collection =>
        mongoClient.GetDatabase("BreakfastDb").GetCollection<ChefNoteDocument>("chef_notes");

    public async Task<ChefNoteResponse> CreateAsync(ChefNoteRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ChefNoteService.Create");

        var doc = new ChefNoteDocument
        {
            NoteId = Guid.NewGuid().ToString(),
            RecipeName = request.RecipeName!,
            ChefName = request.ChefName!,
            NoteText = request.NoteText!,
            Category = request.Category!,
            CreatedAt = DateTime.UtcNow
        };

        await Collection.InsertOneAsync(doc, cancellationToken: cancellationToken);

        logger.LogInformation("Chef note {NoteId} created by {ChefName} for recipe {RecipeName}",
            doc.NoteId, doc.ChefName, doc.RecipeName);

        return MapToResponse(doc);
    }

    public async Task<ChefNoteResponse?> GetByIdAsync(string noteId, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ChefNoteService.GetById");

        var doc = await Collection.Find(x => x.NoteId == noteId)
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : MapToResponse(doc);
    }

    public async Task<ChefNoteResponse?> UpdateAsync(string noteId, UpdateChefNoteRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ChefNoteService.Update");

        var update = Builders<ChefNoteDocument>.Update
            .Set(x => x.NoteText, request.NoteText!)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(request.Category))
            update = update.Set(x => x.Category, request.Category);

        var result = await Collection.FindOneAndUpdateAsync(
            x => x.NoteId == noteId,
            update,
            new FindOneAndUpdateOptions<ChefNoteDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (result is null) return null;

        logger.LogInformation("Chef note {NoteId} updated", noteId);
        return MapToResponse(result);
    }

    public async Task<List<ChefNoteResponse>> ListByRecipeAsync(string recipeName, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ChefNoteService.ListByRecipe");

        var docs = await Collection.Find(x => x.RecipeName == recipeName)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return docs.Select(MapToResponse).ToList();
    }

    private static ChefNoteResponse MapToResponse(ChefNoteDocument doc) => new()
    {
        NoteId = doc.NoteId,
        RecipeName = doc.RecipeName,
        ChefName = doc.ChefName,
        NoteText = doc.NoteText,
        Category = doc.Category,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt
    };
}

public class ChefNoteDocument
{
    [BsonId]
    public string NoteId { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string ChefName { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
