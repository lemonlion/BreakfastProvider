package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code OrderConfig}: caps the number of items allowed on a single order. */
@ConfigurationProperties(prefix = "order")
public class OrderConfig {

    /** Maximum number of items permitted per order (C# default: 10). */
    private int maxItemsPerOrder = 10;

    public int getMaxItemsPerOrder() {
        return maxItemsPerOrder;
    }

    public void setMaxItemsPerOrder(int maxItemsPerOrder) {
        this.maxItemsPerOrder = maxItemsPerOrder;
    }
}
