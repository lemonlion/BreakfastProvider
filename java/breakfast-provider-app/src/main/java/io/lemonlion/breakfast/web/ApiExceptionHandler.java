package io.lemonlion.breakfast.web;

import java.util.List;
import java.util.Map;
import org.springframework.http.HttpStatus;
import org.springframework.http.ProblemDetail;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

/** Renders {@link ValidationException} as an ASP.NET-style validation problem (HTTP 400). */
@RestControllerAdvice
public class ApiExceptionHandler {

    @ExceptionHandler(ValidationException.class)
    public ProblemDetail handleValidation(ValidationException ex) {
        ProblemDetail problem = ProblemDetail.forStatus(HttpStatus.BAD_REQUEST);
        problem.setTitle("One or more validation errors occurred.");
        problem.setProperty("errors", ex.getErrors());
        // Flat list too, so simple test assertions can match message text directly.
        problem.setProperty("messages", ex.getErrors().values().stream().flatMap(List::stream).toList());
        return problem;
    }

    @ExceptionHandler(InvalidStateTransitionException.class)
    public ProblemDetail handleInvalidTransition(InvalidStateTransitionException ex) {
        ProblemDetail problem = ProblemDetail.forStatus(HttpStatus.CONFLICT);
        problem.setTitle("Invalid State Transition");
        problem.setDetail(ex.getMessage());
        return problem;
    }

    @ExceptionHandler(io.lemonlion.breakfast.downstream.DownstreamUnavailableException.class)
    public ProblemDetail handleDownstreamUnavailable(
            io.lemonlion.breakfast.downstream.DownstreamUnavailableException ex) {
        ProblemDetail problem = ProblemDetail.forStatus(HttpStatus.BAD_GATEWAY);
        problem.setTitle(ex.getServiceName() + " Unavailable");
        problem.setDetail(ex.getMessage());
        return problem;
    }

    /** Thrown by the controller when {@code OrderService} reports an invalid status transition. */
    public static class InvalidStateTransitionException extends RuntimeException {
        public InvalidStateTransitionException(String message) {
            super(message);
        }
    }

    /** Exposed for tests that want the canonical empty-errors map shape. */
    public static Map<String, List<String>> emptyErrors() {
        return Map.of();
    }
}
