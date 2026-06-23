package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.RateLimitConfig;
import java.time.Duration;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;
import org.springframework.stereotype.Component;

/**
 * Twin of the C# {@code "OrderCreation"} fixed-window rate limiter policy. A single shared partition,
 * matching the C# {@code RateLimitPartition.GetFixedWindowLimiter("OrderCreation", ...)}.
 */
@Component
public class OrderRateLimiter {

    private final RateLimitConfig config;
    private final AtomicLong windowStartNanos = new AtomicLong(System.nanoTime());
    private final AtomicInteger count = new AtomicInteger();

    public OrderRateLimiter(RateLimitConfig config) {
        this.config = config;
    }

    /** @return true if the request is permitted; false if the window's permit limit is exhausted. */
    public synchronized boolean tryAcquire() {
        long now = System.nanoTime();
        long windowNanos = Duration.ofSeconds(config.getWindowSeconds()).toNanos();
        if (now - windowStartNanos.get() >= windowNanos) {
            windowStartNanos.set(now);
            count.set(0);
        }
        if (count.get() >= config.getPermitLimit()) {
            return false;
        }
        count.incrementAndGet();
        return true;
    }
}
