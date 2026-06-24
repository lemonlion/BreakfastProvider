package io.lemonlion.breakfast.testsupport;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.io.InputStream;
import java.io.UncheckedIOException;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;

/**
 * In-JVM stand-in for the C# Supplier Service fake. Serves {@code GET /ingredients/milk/availability}
 * (status controllable for the Menu "supplier unavailable" scenario) and {@code POST /ingredients/feedback}
 * (recorded, for the customer-feedback consumer scenario).
 */
public final class FakeSupplier {

    private HttpServer server;
    private volatile int availabilityStatus = 200;
    private final FakeHealth health = new FakeHealth();
    private final List<String> feedback = new CopyOnWriteArrayList<>();

    public synchronized void start() {
        if (server != null) {
            return;
        }
        try {
            server = HttpServer.create(new InetSocketAddress("127.0.0.1", 0), 0);
        } catch (IOException e) {
            throw new UncheckedIOException("Failed to start fake supplier", e);
        }
        server.createContext("/ingredients/milk/availability", this::handleAvailability);
        server.createContext("/ingredients/feedback", this::handleFeedback);
        server.createContext("/health", health::handle);
        server.setExecutor(null);
        server.start();
    }

    private void handleAvailability(HttpExchange exchange) throws IOException {
        exchange.sendResponseHeaders(availabilityStatus, -1);
        exchange.close();
    }

    private void handleFeedback(HttpExchange exchange) throws IOException {
        try (InputStream body = exchange.getRequestBody()) {
            feedback.add(new String(body.readAllBytes(), StandardCharsets.UTF_8));
        }
        exchange.sendResponseHeaders(200, -1);
        exchange.close();
    }

    public String url() {
        return "http://127.0.0.1:" + server.getAddress().getPort();
    }

    /** Sets the status returned for the availability check (200 = available, 5xx = unavailable). */
    public void setAvailabilityStatus(int status) {
        this.availabilityStatus = status;
    }

    public boolean receivedFeedback() {
        return !feedback.isEmpty();
    }

    public List<String> feedback() {
        return List.copyOf(feedback);
    }

    /** Controls the status returned by {@code GET /health} (200 by default). */
    public void setHealthStatus(int status) {
        health.setStatus(status);
    }

    public void reset() {
        this.availabilityStatus = 200;
        this.feedback.clear();
        health.reset();
    }
}
