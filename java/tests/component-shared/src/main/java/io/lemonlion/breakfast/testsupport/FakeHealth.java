package io.lemonlion.breakfast.testsupport;

import com.sun.net.httpserver.HttpExchange;
import java.io.IOException;

/** Shared {@code GET /health} handler for the in-JVM fakes, so the SUT's downstream health checks pass. */
final class FakeHealth {

    private FakeHealth() {
    }

    static void ok(HttpExchange exchange) throws IOException {
        exchange.sendResponseHeaders(200, -1);
        exchange.close();
    }
}
