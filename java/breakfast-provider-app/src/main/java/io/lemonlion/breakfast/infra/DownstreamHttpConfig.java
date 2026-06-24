package io.lemonlion.breakfast.infra;

import org.slf4j.MDC;
import org.springframework.boot.web.client.RestTemplateCustomizer;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestTemplate;

/**
 * Propagates the current request's {@code X-Correlation-Id} onto outgoing downstream HTTP calls (twin
 * of the C# header-propagation middleware). Applied to every {@link RestTemplate} built via the
 * auto-configured {@code RestTemplateBuilder} (Kitchen, Supplier, Cow, Goat clients).
 */
@Configuration
public class DownstreamHttpConfig {

    private static final String HEADER = "X-Correlation-Id";
    private static final String MDC_KEY = "correlationId";

    @Bean
    public RestTemplateCustomizer correlationIdPropagatingCustomizer() {
        return (RestTemplate restTemplate) -> restTemplate.getInterceptors().add((request, body, execution) -> {
            String correlationId = MDC.get(MDC_KEY);
            if (correlationId != null && !request.getHeaders().containsKey(HEADER)) {
                request.getHeaders().add(HEADER, correlationId);
            }
            return execution.execute(request, body);
        });
    }
}
