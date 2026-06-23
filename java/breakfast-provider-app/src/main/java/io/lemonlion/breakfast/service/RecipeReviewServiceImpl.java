package io.lemonlion.breakfast.service;

import static com.mongodb.client.model.Filters.eq;

import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoCollection;
import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.bson.Document;
import org.springframework.stereotype.Service;

/** Twin of C# {@code RecipeReviewService}: MongoDB {@code recipe_reviews} collection in {@code BreakfastDb}. */
@Service
public class RecipeReviewServiceImpl implements RecipeReviewService {

    private final MongoClient mongoClient;

    public RecipeReviewServiceImpl(MongoClient mongoClient) {
        this.mongoClient = mongoClient;
    }

    private MongoCollection<Document> collection() {
        return mongoClient.getDatabase("BreakfastDb").getCollection("recipe_reviews");
    }

    @Override
    public RecipeReviewResponse create(RecipeReviewRequest request) {
        String reviewId = UUID.randomUUID().toString();
        Date now = Date.from(Instant.now());
        List<String> tags = request.tags() == null ? List.of() : request.tags();
        Document doc = new Document("_id", reviewId)
                .append("recipeName", request.recipeName())
                .append("reviewerName", request.reviewerName())
                .append("rating", request.rating())
                .append("comments", request.comments() == null ? "" : request.comments())
                .append("tags", tags)
                .append("createdAt", now);
        collection().insertOne(doc);
        return toResponse(doc);
    }

    @Override
    public Optional<RecipeReviewResponse> getById(String reviewId) {
        return Optional.ofNullable(collection().find(eq("_id", reviewId)).first()).map(RecipeReviewServiceImpl::toResponse);
    }

    @Override
    public List<RecipeReviewResponse> listByRecipe(String recipeName) {
        List<RecipeReviewResponse> result = new ArrayList<>();
        collection().find(eq("recipeName", recipeName)).forEach(doc -> result.add(toResponse(doc)));
        return result;
    }

    @SuppressWarnings("unchecked")
    private static RecipeReviewResponse toResponse(Document doc) {
        Date created = doc.getDate("createdAt");
        return new RecipeReviewResponse(
                doc.getString("_id"),
                doc.getString("recipeName"),
                doc.getString("reviewerName"),
                doc.getInteger("rating", 0),
                doc.getString("comments"),
                (List<String>) doc.get("tags", List.class),
                created == null ? null : created.toInstant());
    }
}
