package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.response.MilkResponse;
import io.lemonlion.breakfast.service.MilkSourcingService;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code MilkController} ({@code /milk}) — sources from the Cow service (502 on failure). */
@RestController
@RequestMapping(path = "/milk", produces = MediaType.APPLICATION_JSON_VALUE)
public class MilkController {

    private final MilkSourcingService milkSourcingService;

    public MilkController(MilkSourcingService milkSourcingService) {
        this.milkSourcingService = milkSourcingService;
    }

    @GetMapping
    public MilkResponse getMilk() {
        return milkSourcingService.sourceFromCow();
    }
}
