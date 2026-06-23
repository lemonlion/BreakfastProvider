package io.lemonlion.breakfast;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.ConfigurationPropertiesScan;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * Entry point for the Breakfast Provider SUT — the Spring Boot twin of the C# {@code Program.cs}.
 * Phase 1 wires the Orders domain (REST + Cosmos persistence + outbox/EventGrid + Kafka recipe log).
 */
@SpringBootApplication
@ConfigurationPropertiesScan
@EnableScheduling
public class BreakfastProviderApplication {

    public static void main(String[] args) {
        SpringApplication.run(BreakfastProviderApplication.class, args);
    }
}
