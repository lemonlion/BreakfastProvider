package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code RateLimitConfig}: fixed-window limiter settings for order creation. */
@ConfigurationProperties(prefix = "rate-limit")
public class RateLimitConfig {

    private int permitLimit = 100;
    private int windowSeconds = 60;

    public int getPermitLimit() {
        return permitLimit;
    }

    public void setPermitLimit(int permitLimit) {
        this.permitLimit = permitLimit;
    }

    public int getWindowSeconds() {
        return windowSeconds;
    }

    public void setWindowSeconds(int windowSeconds) {
        this.windowSeconds = windowSeconds;
    }
}
