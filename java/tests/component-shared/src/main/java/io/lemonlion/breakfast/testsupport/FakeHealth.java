package io.lemonlion.breakfast.testsupport;

import com.sun.net.httpserver.HttpExchange;
import java.io.IOException;

/**
 * Controllable {@code GET /health} handler for the in-JVM fakes. Returns 200 by default so the SUT's
 * downstream health checks pass; tests can force a non-2xx status to drive the degraded / downstream-error
 * health-check scenarios.
 */
final class FakeHealth {

    private volatile int status = 200;

    void setStatus(int status) {
        this.status = status;
    }

    void reset() {
        this.status = 200;
    }

    void handle(HttpExchange exchange) throws IOException {
        exchange.sendResponseHeaders(status, -1);
        exchange.close();
    }
}
