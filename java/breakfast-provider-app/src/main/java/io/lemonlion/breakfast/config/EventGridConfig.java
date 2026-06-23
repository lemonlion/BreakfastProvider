package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code EventGridConfig}: endpoint/key/subject for the EventGrid outbox dispatcher. */
@ConfigurationProperties(prefix = "event-grid")
public class EventGridConfig {

    private boolean enabled = true;
    private String endpoint = "";
    private String key = "";
    private String subject = "breakfast/orders";

    public boolean isEnabled() {
        return enabled;
    }

    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
    }

    public String getEndpoint() {
        return endpoint;
    }

    public void setEndpoint(String endpoint) {
        this.endpoint = endpoint;
    }

    public String getKey() {
        return key;
    }

    public void setKey(String key) {
        this.key = key;
    }

    public String getSubject() {
        return subject;
    }

    public void setSubject(String subject) {
        this.subject = subject;
    }
}
