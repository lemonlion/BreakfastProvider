package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import io.lemonlion.breakfast.model.response.InventoryItemResponse;
import io.lemonlion.breakfast.service.InventoryService;
import java.net.URI;
import java.util.List;
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

/** Twin of C# {@code InventoryController} ({@code /inventory}). */
@RestController
@RequestMapping(path = "/inventory", produces = MediaType.APPLICATION_JSON_VALUE)
public class InventoryController {

    private final InventoryService inventoryService;
    private final InventoryValidator validator;

    public InventoryController(InventoryService inventoryService, InventoryValidator validator) {
        this.inventoryService = inventoryService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<InventoryItemResponse> create(@RequestBody InventoryItemRequest request) {
        validator.validate(request);
        InventoryItemResponse response = inventoryService.create(request);
        return ResponseEntity.created(URI.create("/inventory/" + response.id())).body(response);
    }

    @GetMapping("/{id}")
    public ResponseEntity<InventoryItemResponse> getById(@PathVariable int id) {
        return inventoryService.getById(id).map(ResponseEntity::ok).orElseGet(() -> ResponseEntity.notFound().build());
    }

    @GetMapping
    public List<InventoryItemResponse> list() {
        return inventoryService.list();
    }

    @PutMapping(path = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<InventoryItemResponse> update(@PathVariable int id, @RequestBody InventoryItemRequest request) {
        validator.validate(request);
        return inventoryService.update(id, request)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> delete(@PathVariable int id) {
        return inventoryService.delete(id) ? ResponseEntity.noContent().build() : ResponseEntity.notFound().build();
    }
}
