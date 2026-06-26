package io.lemonlion.breakfast.events.kafka;

import io.lemonlion.breakfast.reporting.RecipeCostConsumer;
import java.util.HashMap;
import java.util.Map;
import org.apache.kafka.clients.admin.NewTopic;
import org.apache.kafka.clients.producer.ProducerConfig;
import org.apache.kafka.common.serialization.StringSerializer;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.kafka.config.TopicBuilder;
import org.springframework.kafka.core.DefaultKafkaProducerFactory;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.kafka.core.ProducerFactory;

/** String/String Kafka producer wiring so {@link KafkaRecipeLogPublisher} injects a concrete template type. */
@Configuration
public class KafkaConfiguration {

    // Pre-declare the topics so consumers bind to an existing topic at startup. Without this the topic is
    // created lazily on first publish, and a consumer subscribed beforehand only discovers it after a
    // metadata refresh — delaying the first message's delivery well beyond the test poll windows.
    @Bean
    public NewTopic recipeLogsTopic() {
        return TopicBuilder.name(KafkaRecipeLogger.TOPIC).partitions(1).replicas(1).build();
    }

    @Bean
    public NewTopic recipeCostTopic() {
        return TopicBuilder.name(RecipeCostConsumer.TOPIC).partitions(1).replicas(1).build();
    }

    @Bean
    public ProducerFactory<String, String> recipeLogProducerFactory(
            @Value("${spring.kafka.bootstrap-servers:localhost:9092}") String bootstrapServers) {
        Map<String, Object> config = new HashMap<>();
        config.put(ProducerConfig.BOOTSTRAP_SERVERS_CONFIG, bootstrapServers);
        config.put(ProducerConfig.KEY_SERIALIZER_CLASS_CONFIG, StringSerializer.class);
        config.put(ProducerConfig.VALUE_SERIALIZER_CLASS_CONFIG, StringSerializer.class);
        return new DefaultKafkaProducerFactory<>(config);
    }

    @Bean
    public KafkaTemplate<String, String> kafkaTemplate(ProducerFactory<String, String> recipeLogProducerFactory) {
        return new KafkaTemplate<>(recipeLogProducerFactory);
    }
}
