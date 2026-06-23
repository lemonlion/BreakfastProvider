package io.lemonlion.breakfast.service;

import static com.mongodb.client.model.Filters.eq;
import static com.mongodb.client.model.Updates.combine;
import static com.mongodb.client.model.Updates.set;

import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoCollection;
import com.mongodb.client.model.FindOneAndUpdateOptions;
import com.mongodb.client.model.ReturnDocument;
import com.mongodb.client.model.Sorts;
import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.bson.Document;
import org.springframework.stereotype.Service;

/** Twin of C# {@code ChefNoteService}: MongoDB {@code chef_notes} collection in the {@code BreakfastDb} database. */
@Service
public class ChefNoteServiceImpl implements ChefNoteService {

    private final MongoClient mongoClient;

    public ChefNoteServiceImpl(MongoClient mongoClient) {
        this.mongoClient = mongoClient;
    }

    private MongoCollection<Document> collection() {
        return mongoClient.getDatabase("BreakfastDb").getCollection("chef_notes");
    }

    @Override
    public ChefNoteResponse create(ChefNoteRequest request) {
        String noteId = UUID.randomUUID().toString();
        Date now = Date.from(Instant.now());
        Document doc = new Document("_id", noteId)
                .append("recipeName", request.recipeName())
                .append("chefName", request.chefName())
                .append("noteText", request.noteText())
                .append("category", request.category())
                .append("createdAt", now)
                .append("updatedAt", null);
        collection().insertOne(doc);
        return toResponse(doc);
    }

    @Override
    public Optional<ChefNoteResponse> getById(String noteId) {
        Document doc = collection().find(eq("_id", noteId)).first();
        return Optional.ofNullable(doc).map(ChefNoteServiceImpl::toResponse);
    }

    @Override
    public Optional<ChefNoteResponse> update(String noteId, UpdateChefNoteRequest request) {
        List<org.bson.conversions.Bson> updates = new ArrayList<>();
        updates.add(set("noteText", request.noteText()));
        updates.add(set("updatedAt", Date.from(Instant.now())));
        if (request.category() != null && !request.category().isEmpty()) {
            updates.add(set("category", request.category()));
        }
        Document updated = collection().findOneAndUpdate(eq("_id", noteId), combine(updates),
                new FindOneAndUpdateOptions().returnDocument(ReturnDocument.AFTER));
        return Optional.ofNullable(updated).map(ChefNoteServiceImpl::toResponse);
    }

    @Override
    public List<ChefNoteResponse> listByRecipe(String recipeName) {
        List<ChefNoteResponse> result = new ArrayList<>();
        collection().find(eq("recipeName", recipeName)).sort(Sorts.descending("createdAt"))
                .forEach(doc -> result.add(toResponse(doc)));
        return result;
    }

    private static ChefNoteResponse toResponse(Document doc) {
        return new ChefNoteResponse(
                doc.getString("_id"),
                doc.getString("recipeName"),
                doc.getString("chefName"),
                doc.getString("noteText"),
                doc.getString("category"),
                toInstant(doc.getDate("createdAt")),
                toInstant(doc.getDate("updatedAt")));
    }

    private static Instant toInstant(Date date) {
        return date == null ? null : date.toInstant();
    }
}
