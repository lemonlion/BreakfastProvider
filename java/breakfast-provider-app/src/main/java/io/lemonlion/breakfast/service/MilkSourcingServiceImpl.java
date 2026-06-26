package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.config.DownstreamConfig;
import io.lemonlion.breakfast.downstream.DownstreamUnavailableException;
import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.model.response.MilkResponse;
import java.time.Duration;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;

/** Twin of C# {@code MilkSourcingService}: GET /milk from Cow, GET /goat-milk from Goat; errors → 502. */
@Service
public class MilkSourcingServiceImpl implements MilkSourcingService {

    // A read timeout so a slow/hung downstream surfaces as a RestClientException -> 502 rather than
    // hanging the request (twin of the C# "Cow Service times out -> 502" scenario).
    private static final Duration TIMEOUT = Duration.ofSeconds(2);

    private final RestTemplate restTemplate;
    private final DownstreamConfig config;

    public MilkSourcingServiceImpl(RestTemplateBuilder builder, DownstreamConfig config) {
        this.restTemplate = builder.setConnectTimeout(TIMEOUT).setReadTimeout(TIMEOUT).build();
        this.config = config;
    }

    @Override
    public MilkResponse sourceFromCow() {
        return fetch(config.getCowServiceUrl() + "/milk", MilkResponse.class, "Cow Service");
    }

    @Override
    public GoatMilkResponse sourceFromGoat() {
        return fetch(config.getGoatServiceUrl() + "/goat-milk", GoatMilkResponse.class, "Goat Service");
    }

    private <T> T fetch(String url, Class<T> type, String serviceName) {
        try {
            T body = restTemplate.getForObject(url, type);
            if (body == null) {
                throw new DownstreamUnavailableException(serviceName, "The " + serviceName + " returned an invalid response.");
            }
            return body;
        } catch (RestClientException e) {
            throw new DownstreamUnavailableException(serviceName, "The " + serviceName + " returned an error.");
        }
    }
}
