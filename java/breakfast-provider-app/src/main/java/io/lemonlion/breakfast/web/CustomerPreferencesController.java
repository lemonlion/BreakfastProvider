package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse;
import io.lemonlion.breakfast.service.CustomerPreferenceService;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code CustomerPreferencesController} ({@code /customer-preferences}). */
@RestController
@RequestMapping(path = "/customer-preferences", produces = MediaType.APPLICATION_JSON_VALUE)
public class CustomerPreferencesController {

    private final CustomerPreferenceService preferenceService;
    private final CustomerPreferenceValidator validator;

    public CustomerPreferencesController(CustomerPreferenceService preferenceService,
                                         CustomerPreferenceValidator validator) {
        this.preferenceService = preferenceService;
        this.validator = validator;
    }

    @PutMapping(path = "/{customerId}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public CustomerPreferenceResponse upsert(
            @PathVariable String customerId, @RequestBody CustomerPreferenceRequest request) {
        CustomerPreferenceRequest withId = request.withCustomerId(customerId);
        validator.validate(withId);
        return preferenceService.upsert(withId);
    }

    @GetMapping("/{customerId}")
    public ResponseEntity<CustomerPreferenceResponse> getById(@PathVariable String customerId) {
        return preferenceService.getById(customerId)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }
}
