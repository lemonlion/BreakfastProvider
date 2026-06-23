package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.ReservationRequest;
import io.lemonlion.breakfast.model.response.ReservationResponse;
import io.lemonlion.breakfast.service.ReservationService;
import io.lemonlion.breakfast.web.ApiExceptionHandler.InvalidStateTransitionException;
import java.net.URI;
import java.util.List;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code ReservationsController} ({@code /reservations}). */
@RestController
@RequestMapping(path = "/reservations", produces = MediaType.APPLICATION_JSON_VALUE)
public class ReservationsController {

    private final ReservationService reservationService;
    private final ReservationValidator validator;

    public ReservationsController(ReservationService reservationService, ReservationValidator validator) {
        this.reservationService = reservationService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ReservationResponse> create(@RequestBody ReservationRequest request) {
        validator.validate(request);
        ReservationResponse response = reservationService.create(request);
        return ResponseEntity.created(URI.create("/reservations/" + response.id())).body(response);
    }

    @GetMapping("/{id}")
    public ResponseEntity<ReservationResponse> getById(@PathVariable int id) {
        return reservationService.getById(id).map(ResponseEntity::ok).orElseGet(() -> ResponseEntity.notFound().build());
    }

    @GetMapping
    public List<ReservationResponse> list() {
        return reservationService.list();
    }

    @PutMapping(path = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ReservationResponse> update(@PathVariable int id, @RequestBody ReservationRequest request) {
        validator.validate(request);
        return reservationService.update(id, request)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @PatchMapping("/{id}/cancel")
    public ResponseEntity<ReservationResponse> cancel(@PathVariable int id) {
        ReservationService.CancelResult result = reservationService.cancel(id);
        if (result.notFound()) {
            return ResponseEntity.notFound().build();
        }
        if (result.error() != null) {
            throw new InvalidStateTransitionException(result.error());
        }
        return ResponseEntity.ok(result.reservation());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> delete(@PathVariable int id) {
        return reservationService.delete(id)
                ? ResponseEntity.noContent().build()
                : ResponseEntity.notFound().build();
    }
}
