package io.lemonlion.breakfast.testsupport;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.io.UncheckedIOException;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;

/**
 * In-JVM stand-in for the C# Cow/Goat downstream services. Serves a single GET path returning a fixed
 * JSON body; the status is controllable so the milk-sourcing 502 scenarios can force a failure.
 */
public final class FakeMilkService {

    private final String path;
    private final String jsonBody;
    private HttpServer server;
    private volatile int status = 200;
    private volatile boolean invalidResponse;
    private volatile String lastCorrelationId;
    private final FakeHealth health = new FakeHealth();

    public FakeMilkService(String path, String jsonBody) {
        this.path = path;
        this.jsonBody = jsonBody;
    }

    public synchronized void start() {
        if (server != null) {
            return;
        }
        try {
            server = HttpServer.create(new InetSocketAddress("127.0.0.1", 0), 0);
        } catch (IOException e) {
            throw new UncheckedIOException("Failed to start fake milk service", e);
        }
        server.createContext(path, this::handle);
        server.createContext("/health", health::handle);
        server.setExecutor(null);
        server.start();
    }

    private void handle(HttpExchange exchange) throws IOException {
        lastCorrelationId = exchange.getRequestHeaders().getFirst("X-Correlation-Id");
        if (status != 200) {
            exchange.sendResponseHeaders(status, -1);
            exchange.close();
            return;
        }
        if (invalidResponse) {
            // 200 with an empty body — the SUT deserializes null and treats it as a 502 (invalid response).
            exchange.sendResponseHeaders(200, -1);
            exchange.close();
            return;
        }
        byte[] body = jsonBody.getBytes(StandardCharsets.UTF_8);
        exchange.getResponseHeaders().add("Content-Type", "application/json");
        exchange.sendResponseHeaders(200, body.length);
        exchange.getResponseBody().write(body);
        exchange.close();
    }

    public String url() {
        return "http://127.0.0.1:" + server.getAddress().getPort();
    }

    public void setStatus(int status) {
        this.status = status;
    }

    /** When true, responds 200 with an empty body so the SUT treats it as an invalid downstream response. */
    public void setInvalidResponse(boolean invalidResponse) {
        this.invalidResponse = invalidResponse;
    }

    /** The {@code X-Correlation-Id} header value the SUT forwarded on its last call, or {@code null}. */
    public String lastCorrelationId() {
        return lastCorrelationId;
    }

    /** Controls the status returned by {@code GET /health} (200 by default). */
    public void setHealthStatus(int status) {
        health.setStatus(status);
    }

    public void reset() {
        this.status = 200;
        this.invalidResponse = false;
        this.lastCorrelationId = null;
        health.reset();
    }
}
