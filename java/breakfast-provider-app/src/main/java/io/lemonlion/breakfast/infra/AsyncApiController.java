package io.lemonlion.breakfast.infra;

import java.util.LinkedHashMap;
import java.util.Map;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Twin of the C# AsyncAPI specification endpoint ({@code Bielu.AspNetCore.AsyncApi}). Publishes an
 * AsyncAPI 3.0 document describing the SUT's event channels (Kafka recipe log, Pub/Sub domain events,
 * Event Hubs equipment alerts). Served at {@code /asyncapi/v1.json}.
 */
@RestController
public class AsyncApiController {

    @GetMapping(value = "/asyncapi/v1.json", produces = MediaType.APPLICATION_JSON_VALUE)
    public Map<String, Object> asyncApi() {
        Map<String, Object> doc = new LinkedHashMap<>();
        doc.put("asyncapi", "3.0.0");
        doc.put("defaultContentType", "application/json");

        Map<String, Object> info = new LinkedHashMap<>();
        info.put("title", "Breakfast Provider");
        info.put("version", "1.0.0");
        info.put("description", "Asynchronous messaging API for the Breakfast Provider service.");
        doc.put("info", info);

        doc.put("channels", channels());
        doc.put("operations", operations());
        doc.put("components", components());
        return doc;
    }

    private Map<String, Object> channels() {
        Map<String, Object> channels = new LinkedHashMap<>();
        channels.put("orderCreated", channel("order-created", "OrderCreatedEvent"));
        channels.put("recipeCostCalculated", channel("recipe-cost-calculated", "RecipeCostCalculatedEvent"));
        channels.put("customerFeedback", channel("customer-feedback", "CustomerFeedbackEvent"));
        channels.put("equipmentAlert", channel("equipment-alert", "EquipmentAlertEvent"));
        return channels;
    }

    private Map<String, Object> channel(String address, String messageName) {
        Map<String, Object> channel = new LinkedHashMap<>();
        channel.put("address", address);
        Map<String, Object> messages = new LinkedHashMap<>();
        messages.put(messageName, Map.of("$ref", "#/components/messages/" + messageName));
        channel.put("messages", messages);
        return channel;
    }

    private Map<String, Object> operations() {
        Map<String, Object> operations = new LinkedHashMap<>();
        operations.put("publishOrderCreated", operation("orderCreated"));
        operations.put("publishRecipeCostCalculated", operation("recipeCostCalculated"));
        operations.put("receiveCustomerFeedback", operation("customerFeedback"));
        operations.put("publishEquipmentAlert", operation("equipmentAlert"));
        return operations;
    }

    private Map<String, Object> operation(String channel) {
        Map<String, Object> operation = new LinkedHashMap<>();
        operation.put("action", "send");
        operation.put("channel", Map.of("$ref", "#/channels/" + channel));
        return operation;
    }

    private Map<String, Object> components() {
        Map<String, Object> messages = new LinkedHashMap<>();
        for (String name : new String[] {
                "OrderCreatedEvent", "RecipeCostCalculatedEvent", "CustomerFeedbackEvent", "EquipmentAlertEvent"}) {
            messages.put(name, Map.of("name", name, "contentType", "application/json"));
        }
        return Map.of("messages", messages);
    }
}
