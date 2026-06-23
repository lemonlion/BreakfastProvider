package io.lemonlion.breakfast.web;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/** Carries field-keyed validation messages, rendered as an ASP.NET-style validation problem (400). */
public class ValidationException extends RuntimeException {

    private final Map<String, List<String>> errors;

    public ValidationException(Map<String, List<String>> errors) {
        super("Validation failed");
        this.errors = errors;
    }

    public Map<String, List<String>> getErrors() {
        return errors;
    }

    /** Builder that preserves insertion order (so messages render deterministically). */
    public static final class Collector {
        private final Map<String, List<String>> errors = new LinkedHashMap<>();

        public Collector add(String field, String message) {
            errors.computeIfAbsent(field, k -> new java.util.ArrayList<>()).add(message);
            return this;
        }

        public boolean hasErrors() {
            return !errors.isEmpty();
        }

        public Map<String, List<String>> build() {
            return errors;
        }
    }
}
