package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Pub/Sub settings (project + emulator endpoint + the customer-feedback topic/subscription). */
@ConfigurationProperties(prefix = "pubsub")
public class PubSubConfig {

    private String projectId = "";
    private String emulatorEndpoint = "";
    private String customerFeedbackTopic = "customer-feedback";
    private String customerFeedbackSubscription = "customer-feedback-sub";
    private String batchCompletionTopic = "batch-completions";
    private String batchCompletionSubscription = "batch-completions-sub";

    public boolean isEnabled() {
        return !emulatorEndpoint.isBlank() || !projectId.isBlank();
    }

    public String getProjectId() {
        return projectId;
    }

    public void setProjectId(String projectId) {
        this.projectId = projectId;
    }

    public String getEmulatorEndpoint() {
        return emulatorEndpoint;
    }

    public void setEmulatorEndpoint(String emulatorEndpoint) {
        this.emulatorEndpoint = emulatorEndpoint;
    }

    public String getCustomerFeedbackTopic() {
        return customerFeedbackTopic;
    }

    public void setCustomerFeedbackTopic(String customerFeedbackTopic) {
        this.customerFeedbackTopic = customerFeedbackTopic;
    }

    public String getCustomerFeedbackSubscription() {
        return customerFeedbackSubscription;
    }

    public void setCustomerFeedbackSubscription(String customerFeedbackSubscription) {
        this.customerFeedbackSubscription = customerFeedbackSubscription;
    }

    public String getBatchCompletionTopic() {
        return batchCompletionTopic;
    }

    public void setBatchCompletionTopic(String batchCompletionTopic) {
        this.batchCompletionTopic = batchCompletionTopic;
    }

    public String getBatchCompletionSubscription() {
        return batchCompletionSubscription;
    }

    public void setBatchCompletionSubscription(String batchCompletionSubscription) {
        this.batchCompletionSubscription = batchCompletionSubscription;
    }
}
