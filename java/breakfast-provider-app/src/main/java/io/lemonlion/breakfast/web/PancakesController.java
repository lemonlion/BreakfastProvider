package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;
import io.lemonlion.breakfast.service.PancakeService;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code PancakesController} ({@code /pancakes}). */
@RestController
@RequestMapping(path = "/pancakes", produces = MediaType.APPLICATION_JSON_VALUE)
public class PancakesController {

    private final PancakeService pancakeService;
    private final PancakeValidator validator;

    public PancakesController(PancakeService pancakeService, PancakeValidator validator) {
        this.pancakeService = pancakeService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<PancakeResponse> makePancakes(@RequestBody PancakeRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(pancakeService.makePancakes(request));
    }
}
