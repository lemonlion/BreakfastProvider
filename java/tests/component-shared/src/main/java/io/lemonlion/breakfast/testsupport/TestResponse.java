package io.lemonlion.breakfast.testsupport;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.JsonNode;

/** A captured HTTP response with helpers to deserialize the JSON body into test models. */
public record TestResponse(int status, String body) {

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
