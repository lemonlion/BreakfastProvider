package io.lemonlion.breakfast.downstream;

import io.lemonlion.breakfast.config.DownstreamConfig;
import io.lemonlion.breakfast.model.request.OrderRequest;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestTemplate;

/**
 * Twin of the C# Kitchen Service HTTP call. Uses {@link RestTemplate} so the Kronikol4J Spring HTTP
 * interceptor records the exchange in the report's sequence diagram.
 */
@Component
public class HttpKitchenClient implements KitchenClient {

    private final RestTemplate restTemplate;
    private final String baseUrl;

    public HttpKitchenClient(RestTemplateBuilder builder, DownstreamConfig config) {
        this.restTemplate = builder.build();
        this.baseUrl = config.getKitchenServiceUrl();
    }

    @Override
    public void requestPreparation(UUID orderId, OrderRequest order) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("orderId", orderId);
        body.put("items", order.items().stream()
                .map(i -> Map.of(
                        "itemType", i.itemType() == null ? "" : i.itemType(),
                        "quantity", i.effectiveQuantity()))
                .toList());
        restTemplate.postForEntity(baseUrl + "/prepare", body, Void.class);
    }
}
