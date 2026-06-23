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
 * An in-JVM stand-in for the C# Kitchen Service fake. The SUT's {@code HttpKitchenClient} POSTs to
 * {@code /prepare}; Kronikol4J's HTTP interceptor records that outbound call. The response status is
 * controllable per test (e.g. {@code 503} for the "kitchen busy" scenario).
 */
public final class FakeKitchen {

    private HttpServer server;
    private volatile int nextStatus = 200;
    private final List<String> preparations = new CopyOnWriteArrayList<>();

    public synchronized void start() {
        if (server != null) {
            return;
        }
        try {
            server = HttpServer.create(new InetSocketAddress("127.0.0.1", 0), 0);
        } catch (IOException e) {
            throw new UncheckedIOException("Failed to start fake kitchen", e);
        }
        server.createContext("/prepare", this::handlePrepare);
        server.createContext("/health", FakeHealth::ok);
        server.setExecutor(null);
        server.start();
    }

    private void handlePrepare(HttpExchange exchange) throws IOException {
        try (InputStream body = exchange.getRequestBody()) {
            preparations.add(new String(body.readAllBytes(), StandardCharsets.UTF_8));
        }
        int status = nextStatus;
        exchange.sendResponseHeaders(status, -1);
        exchange.close();
    }

    public String url() {
        return "http://127.0.0.1:" + server.getAddress().getPort();
    }

    /** Sets the HTTP status the kitchen returns on the next (and subsequent) calls. */
    public void setNextStatus(int status) {
        this.nextStatus = status;
    }

    public void reset() {
        this.nextStatus = 200;
        this.preparations.clear();
    }

    public boolean receivedPreparation() {
        return !preparations.isEmpty();
    }

    public List<String> preparations() {
        return List.copyOf(preparations);
    }
}
