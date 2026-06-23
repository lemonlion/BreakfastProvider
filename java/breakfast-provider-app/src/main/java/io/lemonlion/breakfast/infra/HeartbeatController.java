package io.lemonlion.breakfast.infra;

import java.util.Map;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Twin of the C# {@code HeartbeatController}: the root endpoint returns a tiny liveness document so an
 * external probe can confirm the service is up without exercising any dependency.
 */
@RestController
public class HeartbeatController {

    @GetMapping("/")
    public Map<String, String> heartbeat() {
        return Map.of("status", "ok");
    }
}
