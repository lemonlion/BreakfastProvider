package io.lemonlion.breakfast.testsupport;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.io.UncheckedIOException;
import java.net.InetSocketAddress;

/**
 * In-JVM stand-in for the C# Supplier Service fake. Serves {@code GET /ingredients/milk/availability};
 * the returned status is controllable so the Menu "supplier unavailable" scenario can force a failure.
 */
public final class FakeSupplier {

    private HttpServer server;
    private volatile int availabilityStatus = 200;

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
        server.setExecutor(null);
        server.start();
    }

    private void handleAvailability(HttpExchange exchange) throws IOException {
        exchange.sendResponseHeaders(availabilityStatus, -1);
        exchange.close();
    }

    public String url() {
        return "http://127.0.0.1:" + server.getAddress().getPort();
    }

    /** Sets the status returned for the availability check (200 = available, 5xx = unavailable). */
    public void setAvailabilityStatus(int status) {
        this.availabilityStatus = status;
    }

    public void reset() {
        this.availabilityStatus = 200;
    }
}
