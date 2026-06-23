package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.IngredientWasteRequest;
import io.lemonlion.breakfast.model.response.IngredientWasteResponse;
import io.lemonlion.breakfast.service.IngredientWasteService;
import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code IngredientWasteController} ({@code /ingredient-waste}). */
@RestController
@RequestMapping(path = "/ingredient-waste", produces = MediaType.APPLICATION_JSON_VALUE)
public class IngredientWasteController {

    private final IngredientWasteService service;
    private final IngredientWasteValidator validator;

    public IngredientWasteController(IngredientWasteService service, IngredientWasteValidator validator) {
        this.service = service;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IngredientWasteResponse> record(@RequestBody IngredientWasteRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(service.record(request));
    }

    @GetMapping("/recipe/{recipeName}")
    public List<IngredientWasteResponse> listByRecipe(@PathVariable String recipeName) {
        return service.listByRecipe(recipeName);
    }

    @DeleteMapping("/{wasteId}")
    public ResponseEntity<Void> delete(@PathVariable String wasteId) {
        service.delete(wasteId);
        return ResponseEntity.noContent().build();
    }
}
