package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.WaffleRequest;
import io.lemonlion.breakfast.model.response.WaffleResponse;
import io.lemonlion.breakfast.service.WaffleService;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code WafflesController} ({@code /waffles}). */
@RestController
@RequestMapping(path = "/waffles", produces = MediaType.APPLICATION_JSON_VALUE)
public class WafflesController {

    private final WaffleService waffleService;
    private final WaffleValidator validator;

    public WafflesController(WaffleService waffleService, WaffleValidator validator) {
        this.waffleService = waffleService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<WaffleResponse> makeWaffles(@RequestBody WaffleRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(waffleService.makeWaffles(request));
    }
}
