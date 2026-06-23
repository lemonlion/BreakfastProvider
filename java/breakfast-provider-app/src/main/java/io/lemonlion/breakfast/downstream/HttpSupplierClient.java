package io.lemonlion.breakfast.downstream;

import io.lemonlion.breakfast.config.DownstreamConfig;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.http.HttpStatusCode;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;

/** Twin of the C# Supplier availability check: GET {@code ingredients/milk/availability}. */
@Component
public class HttpSupplierClient implements SupplierClient {

    private static final Logger log = LoggerFactory.getLogger(HttpSupplierClient.class);

    private final RestTemplate restTemplate;
    private final String baseUrl;

    public HttpSupplierClient(RestTemplateBuilder builder, DownstreamConfig config) {
        this.restTemplate = builder.build();
        this.baseUrl = config.getSupplierServiceUrl();
    }

    @Override
    public boolean isMilkAvailable() {
        try {
            HttpStatusCode status = restTemplate
                    .getForEntity(baseUrl + "/ingredients/milk/availability", Void.class)
                    .getStatusCode();
            return status.is2xxSuccessful();
        } catch (RestClientException ex) {
            log.warn("Supplier service is unavailable; marking ingredients unavailable", ex);
            return false;
        }
    }
}
