package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;
import io.lemonlion.breakfast.service.ChefNoteService;
import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code ChefNotesController} ({@code /chef-notes}). */
@RestController
@RequestMapping(path = "/chef-notes", produces = MediaType.APPLICATION_JSON_VALUE)
public class ChefNotesController {

    private final ChefNoteService chefNoteService;
    private final ChefNoteValidator validator;

    public ChefNotesController(ChefNoteService chefNoteService, ChefNoteValidator validator) {
        this.chefNoteService = chefNoteService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ChefNoteResponse> create(@RequestBody ChefNoteRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(chefNoteService.create(request));
    }

    @GetMapping("/{noteId}")
    public ResponseEntity<ChefNoteResponse> getById(@PathVariable String noteId) {
        return chefNoteService.getById(noteId).map(ResponseEntity::ok).orElseGet(() -> ResponseEntity.notFound().build());
    }

    @PatchMapping(path = "/{noteId}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ChefNoteResponse> update(
            @PathVariable String noteId, @RequestBody UpdateChefNoteRequest request) {
        validator.validate(request);
        return chefNoteService.update(noteId, request)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @GetMapping("/recipe/{recipeName}")
    public List<ChefNoteResponse> listByRecipe(@PathVariable String recipeName) {
        return chefNoteService.listByRecipe(recipeName);
    }
}
