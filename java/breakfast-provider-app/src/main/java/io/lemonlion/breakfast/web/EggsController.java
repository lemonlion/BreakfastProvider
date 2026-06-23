package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.response.EggsResponse;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code EggsController} ({@code /eggs}) — returns a static ingredient response. */
@RestController
@RequestMapping(path = "/eggs", produces = MediaType.APPLICATION_JSON_VALUE)
public class EggsController {

    @GetMapping
    public EggsResponse getEggs() {
        return new EggsResponse();
    }
}
