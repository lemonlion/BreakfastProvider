package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest;
import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import io.lemonlion.breakfast.service.DailySpecialsService;
import io.lemonlion.breakfast.service.DailySpecialsService.Special;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ProblemDetail;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code DailySpecialsController} ({@code /daily-specials}). */
@RestController
@RequestMapping(path = "/daily-specials", produces = MediaType.APPLICATION_JSON_VALUE)
public class DailySpecialsController {

    private final DailySpecialsService service;
    private final DailySpecialOrderValidator validator;

    public DailySpecialsController(DailySpecialsService service, DailySpecialOrderValidator validator) {
        this.service = service;
        this.validator = validator;
    }

    @GetMapping
    public List<DailySpecialResponse> getDailySpecials() {
        return service.getAvailableSpecials();
    }

    @PostMapping(path = "/orders", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> orderDailySpecial(
            @RequestBody DailySpecialOrderRequest request,
            @RequestHeader(value = "Idempotency-Key", required = false) String idempotencyKey) {

        Optional<DailySpecialOrderResponse> cached = service.checkIdempotency(idempotencyKey);
        if (cached.isPresent()) {
            return ResponseEntity.status(HttpStatus.CREATED).body(cached.get());
        }

        validator.validate(request);

        Optional<Special> special = service.validateSpecialExists(request.specialId());
        if (special.isEmpty()) {
            ProblemDetail problem = ProblemDetail.forStatus(HttpStatus.NOT_FOUND);
            problem.setTitle("Daily special not found");
            problem.setDetail("No daily special found with ID '" + request.specialId() + "'.");
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(problem);
        }

        Optional<DailySpecialOrderResponse> response =
                service.reserveQuantity(request.specialId(), request.quantity(), special.get().name());
        if (response.isEmpty()) {
            ProblemDetail problem = ProblemDetail.forStatus(HttpStatus.CONFLICT);
            problem.setTitle("Daily special sold out");
            problem.setDetail("'" + special.get().name() + "' has reached the maximum orders for today.");
            return ResponseEntity.status(HttpStatus.CONFLICT).body(problem);
        }

        service.storeIdempotencyResult(idempotencyKey, response.get());
        service.publishOrderEvent(response.get(), special.get().name());
        return ResponseEntity.status(HttpStatus.CREATED).body(response.get());
    }

    @DeleteMapping("/orders")
    public ResponseEntity<Void> resetOrderCounts(@RequestParam(required = false) UUID specialId) {
        service.resetOrderCounts(specialId);
        return ResponseEntity.noContent().build();
    }
}
