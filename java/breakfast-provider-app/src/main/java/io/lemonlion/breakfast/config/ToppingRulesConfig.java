package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code ToppingRulesConfig}: caps toppings per item. */
@ConfigurationProperties(prefix = "topping-rules")
public class ToppingRulesConfig {

    private int maxToppingsPerItem = 5;

    public int getMaxToppingsPerItem() {
        return maxToppingsPerItem;
    }

    public void setMaxToppingsPerItem(int maxToppingsPerItem) {
        this.maxToppingsPerItem = maxToppingsPerItem;
    }
}
