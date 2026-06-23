package io.lemonlion.breakfast.testsupport;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;
import java.util.Map;

/** A captured HTTP response with helpers to deserialize the JSON body into test models. */
public record TestResponse(int status, String body, Map<String, List<String>> headers) {

    /** Convenience for responses where headers are not asserted. */
    public TestResponse(int status, String body) {
        this(status, body, Map.of());
    }

    /** The first value of a response header (case-insensitive), or {@code null} if absent. */
    public String header(String name) {
        for (Map.Entry<String, List<String>> entry : headers.entrySet()) {
            if (entry.getKey().equalsIgnoreCase(name) && !entry.getValue().isEmpty()) {
                return entry.getValue().get(0);
            }
        }
        return null;
    }

    public <T> T as(Class<T> type) {
        try {
            return JsonMappers.instance().readValue(body, type);
        } catch (Exception e) {
            throw new IllegalStateException("Failed to deserialize response body: " + body, e);
        }
    }

    public <T> T as(TypeReference<T> type) {
        try {
            return JsonMappers.instance().readValue(body, type);
        } catch (Exception e) {
            throw new IllegalStateException("Failed to deserialize response body: " + body, e);
        }
    }

    public JsonNode json() {
        try {
            return JsonMappers.instance().readTree(body);
        } catch (Exception e) {
            throw new IllegalStateException("Failed to parse response body: " + body, e);
        }
    }

    public boolean bodyContains(String text) {
        return body != null && body.contains(text);
    }
}
