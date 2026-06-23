package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.response.MuffinResponse;
import io.lemonlion.breakfast.service.MuffinService;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code MuffinsController} ({@code /muffins}). */
@RestController
@RequestMapping(path = "/muffins", produces = MediaType.APPLICATION_JSON_VALUE)
public class MuffinsController {

    private final MuffinService muffinService;
    private final MuffinValidator validator;

    public MuffinsController(MuffinService muffinService, MuffinValidator validator) {
        this.muffinService = muffinService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<MuffinResponse> makeMuffins(@RequestBody MuffinRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(muffinService.makeMuffins(request));
    }
}
