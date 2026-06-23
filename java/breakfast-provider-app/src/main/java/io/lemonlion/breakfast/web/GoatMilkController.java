package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.FeatureSwitchesConfig;
import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.service.MilkSourcingService;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ProblemDetail;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code GoatMilkController} ({@code /goat-milk}) — feature-gated; sources from the Goat service. */
@RestController
@RequestMapping(path = "/goat-milk", produces = MediaType.APPLICATION_JSON_VALUE)
public class GoatMilkController {

    private final MilkSourcingService milkSourcingService;
    private final FeatureSwitchesConfig featureSwitches;

    public GoatMilkController(MilkSourcingService milkSourcingService, FeatureSwitchesConfig featureSwitches) {
        this.milkSourcingService = milkSourcingService;
        this.featureSwitches = featureSwitches;
    }

    @GetMapping
    public ResponseEntity<?> getGoatMilk() {
        if (!featureSwitches.isGoatMilkEnabled()) {
            ProblemDetail problem = ProblemDetail.forStatus(HttpStatus.NOT_FOUND);
            problem.setTitle("Feature Disabled");
            problem.setDetail("Goat milk is not currently available.");
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(problem);
        }
        GoatMilkResponse response = milkSourcingService.sourceFromGoat();
        return ResponseEntity.ok(response);
    }
}
