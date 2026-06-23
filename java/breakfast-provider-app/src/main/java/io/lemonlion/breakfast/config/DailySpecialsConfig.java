package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code DailySpecialsConfig}. */
@ConfigurationProperties(prefix = "daily-specials")
public class DailySpecialsConfig {

    private int maxOrdersPerSpecial = 100;
    private int idempotencyTtlSeconds = 86400;

    public int getMaxOrdersPerSpecial() {
        return maxOrdersPerSpecial;
    }

    public void setMaxOrdersPerSpecial(int maxOrdersPerSpecial) {
        this.maxOrdersPerSpecial = maxOrdersPerSpecial;
    }

    public int getIdempotencyTtlSeconds() {
        return idempotencyTtlSeconds;
    }

    public void setIdempotencyTtlSeconds(int idempotencyTtlSeconds) {
        this.idempotencyTtlSeconds = idempotencyTtlSeconds;
    }
}
