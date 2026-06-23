package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import io.lemonlion.breakfast.service.ToppingService;
import java.util.List;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code ToppingsController} ({@code /toppings}). */
@RestController
@RequestMapping(path = "/toppings", produces = MediaType.APPLICATION_JSON_VALUE)
public class ToppingsController {

    private final ToppingService toppingService;
    private final ToppingValidator validator;

    public ToppingsController(ToppingService toppingService, ToppingValidator validator) {
        this.toppingService = toppingService;
        this.validator = validator;
    }

    @GetMapping
    public List<ToppingResponse> getToppings() {
        return toppingService.getAvailableToppings();
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ToppingResponse> addTopping(@RequestBody ToppingRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(toppingService.createTopping(request));
    }

    @PutMapping(path = "/{toppingId}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ToppingResponse> updateTopping(
            @PathVariable UUID toppingId, @RequestBody UpdateToppingRequest request) {
        validator.validate(request);
        return toppingService.updateTopping(toppingId, request)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @DeleteMapping("/{toppingId}")
    public ResponseEntity<Void> deleteTopping(@PathVariable UUID toppingId) {
        return toppingService.deleteTopping(toppingId)
                ? ResponseEntity.noContent().build()
                : ResponseEntity.notFound().build();
    }
}
