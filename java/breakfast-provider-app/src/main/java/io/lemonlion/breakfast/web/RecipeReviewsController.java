package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.RecipeReviewRequest;
import io.lemonlion.breakfast.model.response.RecipeReviewResponse;
import io.lemonlion.breakfast.service.RecipeReviewService;
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

/** Twin of C# {@code RecipeReviewsController} ({@code /recipe-reviews}). */
@RestController
@RequestMapping(path = "/recipe-reviews", produces = MediaType.APPLICATION_JSON_VALUE)
public class RecipeReviewsController {

    private final RecipeReviewService recipeReviewService;
    private final RecipeReviewValidator validator;

    public RecipeReviewsController(RecipeReviewService recipeReviewService, RecipeReviewValidator validator) {
        this.recipeReviewService = recipeReviewService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<RecipeReviewResponse> create(@RequestBody RecipeReviewRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(recipeReviewService.create(request));
    }

    @GetMapping("/{reviewId}")
    public ResponseEntity<RecipeReviewResponse> getById(@PathVariable String reviewId) {
        return recipeReviewService.getById(reviewId).map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @GetMapping("/recipe/{recipeName}")
    public List<RecipeReviewResponse> listByRecipe(@PathVariable String recipeName) {
        return recipeReviewService.listByRecipe(recipeName);
    }
}
