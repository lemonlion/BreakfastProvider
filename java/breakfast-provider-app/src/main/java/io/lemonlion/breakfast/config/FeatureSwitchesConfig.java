package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code FeatureSwitchesConfig}. */
@ConfigurationProperties(prefix = "feature-switches")
public class FeatureSwitchesConfig {

    private boolean raspberryToppingEnabled = true;
    private boolean goatMilkEnabled = true;

    public boolean isRaspberryToppingEnabled() {
        return raspberryToppingEnabled;
    }

    public void setRaspberryToppingEnabled(boolean raspberryToppingEnabled) {
        this.raspberryToppingEnabled = raspberryToppingEnabled;
    }

    public boolean isGoatMilkEnabled() {
        return goatMilkEnabled;
    }

    public void setGoatMilkEnabled(boolean goatMilkEnabled) {
        this.goatMilkEnabled = goatMilkEnabled;
    }
}
