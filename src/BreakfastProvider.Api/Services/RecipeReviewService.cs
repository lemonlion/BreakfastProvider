using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BreakfastProvider.Api.Services;

public interface IRecipeReviewService
{
    Task<RecipeReviewResponse> CreateAsync(RecipeReviewRequest request, CancellationToken cancellationToken = default);
    Task<RecipeReviewResponse?> GetByIdAsync(string reviewId, CancellationToken cancellationToken = default);
    Task<List<RecipeReviewResponse>> ListByRecipeAsync(string recipeName, CancellationToken cancellationToken = default);
}

public class RecipeReviewService(IMongoClient mongoClient, ILogger<RecipeReviewService> logger) : IRecipeReviewService
{
    private IMongoCollection<RecipeReviewDocument> Collection =>
        mongoClient.GetDatabase("BreakfastDb").GetCollection<RecipeReviewDocument>("recipe_reviews");

    public async Task<RecipeReviewResponse> CreateAsync(RecipeReviewRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("RecipeReviewService.Create");

        var doc = new RecipeReviewDocument
        {
            ReviewId = Guid.NewGuid().ToString(),
            RecipeName = request.RecipeName!,
            ReviewerName = request.ReviewerName!,
            Rating = request.Rating,
            Comments = request.Comments ?? string.Empty,
            Tags = request.Tags ?? [],
            CreatedAt = DateTime.UtcNow
        };

        await Collection.InsertOneAsync(doc, cancellationToken: cancellationToken);

        logger.LogInformation("Recipe review {ReviewId} created for {RecipeName}", doc.ReviewId, doc.RecipeName);

        return MapToResponse(doc);
    }

    public async Task<RecipeReviewResponse?> GetByIdAsync(string reviewId, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("RecipeReviewService.GetById");

        var doc = await Collection.Find(x => x.ReviewId == reviewId)
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : MapToResponse(doc);
    }

    public async Task<List<RecipeReviewResponse>> ListByRecipeAsync(string recipeName, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("RecipeReviewService.ListByRecipe");

        var docs = await Collection.Find(x => x.RecipeName == recipeName)
            .ToListAsync(cancellationToken);

        return docs.Select(MapToResponse).ToList();
    }

    private static RecipeReviewResponse MapToResponse(RecipeReviewDocument doc) => new()
    {
        ReviewId = doc.ReviewId,
        RecipeName = doc.RecipeName,
        ReviewerName = doc.ReviewerName,
        Rating = doc.Rating,
        Comments = doc.Comments,
        Tags = doc.Tags,
        CreatedAt = doc.CreatedAt
    };
}

public class RecipeReviewDocument
{
    [BsonId]
    public string ReviewId { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
