package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.response.FlourResponse;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code FlourController} ({@code /flour}) — returns a static ingredient response. */
@RestController
@RequestMapping(path = "/flour", produces = MediaType.APPLICATION_JSON_VALUE)
public class FlourController {

    @GetMapping
    public FlourResponse getFlour() {
        return new FlourResponse();
    }
}
