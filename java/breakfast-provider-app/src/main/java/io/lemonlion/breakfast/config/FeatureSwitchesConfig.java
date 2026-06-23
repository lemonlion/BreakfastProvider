package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code FeatureSwitchesConfig}. */
@ConfigurationProperties(prefix = "feature-switches")
public class FeatureSwitchesConfig {

    private boolean raspberryToppingEnabled = true;

    public boolean isRaspberryToppingEnabled() {
        return raspberryToppingEnabled;
    }

    public void setRaspberryToppingEnabled(boolean raspberryToppingEnabled) {
        this.raspberryToppingEnabled = raspberryToppingEnabled;
    }
}
