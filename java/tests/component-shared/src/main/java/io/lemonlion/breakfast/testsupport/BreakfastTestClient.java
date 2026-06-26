package io.lemonlion.breakfast.testsupport;

import io.kronikol.core.constants.TrackingHeaders;
import io.kronikol.core.context.TestIdentityScope;
import io.kronikol.core.context.TestInfo;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpRequest.BodyPublishers;
import java.net.http.HttpResponse;
import java.net.http.HttpResponse.BodyHandlers;
import java.nio.charset.StandardCharsets;

/**
 * HTTP client for driving the SUT in component tests. Stamps the Kronikol4J test-identity headers from
 * the current {@link TestIdentityScope} so the SUT's server-side filter attributes everything it does
 * (Cosmos, Kafka, downstream HTTP) to the running test — exactly how the C# test client behaves.
 */
public final class BreakfastTestClient {

    private final String baseUrl;
    private final HttpClient httpClient;

    public BreakfastTestClient(String baseUrl) {
        this.baseUrl = baseUrl;
        this.httpClient = HttpClient.newHttpClient();
    }

    public TestResponse post(String path, Object body) {
        return send(requestBuilder(path).POST(jsonBody(body)).header("Content-Type", "application/json"));
    }

    public TestResponse post(String path, Object body, java.util.Map<String, String> headers) {
        HttpRequest.Builder builder = requestBuilder(path).POST(jsonBody(body)).header("Content-Type", "application/json");
        headers.forEach(builder::header);
        return send(builder);
    }

    /**
     * POSTs a raw string body with an explicit {@code Content-Type}, for the unsupported-media-type
     * scenarios (e.g. text/plain against a JSON-only endpoint -> 415).
     */
    public TestResponse postRaw(String path, String body, String contentType) {
        return send(requestBuilder(path)
                .POST(BodyPublishers.ofString(body, StandardCharsets.UTF_8))
                .header("Content-Type", contentType));
    }

    public TestResponse get(String path) {
        return send(requestBuilder(path).GET());
    }

    public TestResponse get(String path, java.util.Map<String, String> headers) {
        HttpRequest.Builder builder = requestBuilder(path).GET();
        headers.forEach(builder::header);
        return send(builder);
    }

    public TestResponse patch(String path, Object body) {
        return send(requestBuilder(path).method("PATCH", jsonBody(body)).header("Content-Type", "application/json"));
    }

    public TestResponse put(String path, Object body) {
        return send(requestBuilder(path).PUT(jsonBody(body)).header("Content-Type", "application/json"));
    }

    public TestResponse delete(String path) {
        return send(requestBuilder(path).DELETE());
    }

    private HttpRequest.Builder requestBuilder(String path) {
        HttpRequest.Builder builder = HttpRequest.newBuilder(URI.create(baseUrl + path)).header("Accept", "application/json");
        TestInfo identity = TestIdentityScope.current();
        if (identity != null) {
            builder.header(TrackingHeaders.CURRENT_TEST_NAME, identity.name());
            if (identity.id() != null) {
                builder.header(TrackingHeaders.CURRENT_TEST_ID, String.valueOf(identity.id()));
            }
        }
        return builder;
    }

    private static HttpRequest.BodyPublisher jsonBody(Object body) {
        try {
            return BodyPublishers.ofString(JsonMappers.instance().writeValueAsString(body), StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new IllegalStateException("Failed to serialize request body", e);
        }
    }

    private TestResponse send(HttpRequest.Builder builder) {
        try {
            HttpResponse<String> response = httpClient.send(builder.build(), BodyHandlers.ofString());
            return new TestResponse(response.statusCode(), response.body(), response.headers().map());
        } catch (Exception e) {
            throw new IllegalStateException("HTTP request failed", e);
        }
    }
}
