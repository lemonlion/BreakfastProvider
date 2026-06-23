package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;
import java.util.List;
import java.util.Optional;

/** Twin of C# {@code IChefNoteService} (MongoDB-backed). */
public interface ChefNoteService {

    ChefNoteResponse create(ChefNoteRequest request);

    Optional<ChefNoteResponse> getById(String noteId);

    Optional<ChefNoteResponse> update(String noteId, UpdateChefNoteRequest request);

    List<ChefNoteResponse> listByRecipe(String recipeName);
}
