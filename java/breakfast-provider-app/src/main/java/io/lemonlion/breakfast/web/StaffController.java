package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;
import io.lemonlion.breakfast.service.StaffService;
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

/** Twin of C# {@code StaffController} ({@code /staff}). */
@RestController
@RequestMapping(path = "/staff", produces = MediaType.APPLICATION_JSON_VALUE)
public class StaffController {

    private final StaffService staffService;
    private final StaffValidator validator;

    public StaffController(StaffService staffService, StaffValidator validator) {
        this.staffService = staffService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<StaffMemberResponse> create(@RequestBody StaffMemberRequest request) {
        validator.validate(request);
        StaffMemberResponse response = staffService.create(request);
        return ResponseEntity.created(URI.create("/staff/" + response.id())).body(response);
    }

    @GetMapping("/{id}")
    public ResponseEntity<StaffMemberResponse> getById(@PathVariable int id) {
        return staffService.getById(id).map(ResponseEntity::ok).orElseGet(() -> ResponseEntity.notFound().build());
    }

    @GetMapping
    public List<StaffMemberResponse> list() {
        return staffService.list();
    }

    @PutMapping(path = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<StaffMemberResponse> update(@PathVariable int id, @RequestBody StaffMemberRequest request) {
        validator.validate(request);
        return staffService.update(id, request)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> delete(@PathVariable int id) {
        return staffService.delete(id) ? ResponseEntity.noContent().build() : ResponseEntity.notFound().build();
    }
}
