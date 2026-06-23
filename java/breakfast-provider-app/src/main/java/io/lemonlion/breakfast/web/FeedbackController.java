package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import io.lemonlion.breakfast.service.FeedbackService;
import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code FeedbackController} ({@code /feedback}). */
@RestController
@RequestMapping(path = "/feedback", produces = MediaType.APPLICATION_JSON_VALUE)
public class FeedbackController {

    private final FeedbackService feedbackService;
    private final FeedbackValidator validator;

    public FeedbackController(FeedbackService feedbackService, FeedbackValidator validator) {
        this.feedbackService = feedbackService;
        this.validator = validator;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<FeedbackResponse> create(@RequestBody FeedbackRequest request) {
        validator.validate(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(feedbackService.create(request));
    }

    @GetMapping("/{feedbackId}")
    public ResponseEntity<FeedbackResponse> getById(@PathVariable String feedbackId) {
        return feedbackService.getById(feedbackId).map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @GetMapping("/order/{orderId}")
    public List<FeedbackResponse> listByOrder(@PathVariable String orderId) {
        return feedbackService.listByOrder(orderId);
    }
}
