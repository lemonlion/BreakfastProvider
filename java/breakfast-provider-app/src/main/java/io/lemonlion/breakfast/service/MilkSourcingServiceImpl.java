package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.config.DownstreamConfig;
import io.lemonlion.breakfast.downstream.DownstreamUnavailableException;
import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.model.response.MilkResponse;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;

/** Twin of C# {@code MilkSourcingService}: GET /milk from Cow, GET /goat-milk from Goat; errors → 502. */
@Service
public class MilkSourcingServiceImpl implements MilkSourcingService {

    private final RestTemplate restTemplate;
    private final DownstreamConfig config;

    public MilkSourcingServiceImpl(RestTemplateBuilder builder, DownstreamConfig config) {
        this.restTemplate = builder.build();
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
