package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import io.lemonlion.breakfast.model.response.IngredientUsageResponse;
import io.lemonlion.breakfast.model.response.IngredientUsageSummaryResponse;
import io.lemonlion.breakfast.service.IngredientUsageService;
import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code IngredientUsageController} ({@code /ingredient-usage}). */
@RestController
@RequestMapping(path = "/ingredient-usage", produces = MediaType.APPLICATION_JSON_VALUE)
public class IngredientUsageController {

    private final IngredientUsageService service;
    private final IngredientUsageValidator validator;

    public IngredientUsageController(IngredientUsageService service, IngredientUsageValidator validator) {
        this.service = service;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IngredientUsageResponse> record(@RequestBody IngredientUsageRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(service.record(request));
    }

    @GetMapping("/summary")
    public List<IngredientUsageSummaryResponse> getSummary() {
        return service.getSummary();
    }

    @GetMapping("/ingredient/{ingredientName}")
    public List<IngredientUsageResponse> listByIngredient(@PathVariable String ingredientName) {
        return service.listByIngredient(ingredientName);
    }
}
