package io.lemonlion.breakfast.persistence.bigquery;

import com.google.auth.oauth2.GoogleCredentials;
import com.google.cloud.NoCredentials;
import com.google.cloud.bigquery.BigQuery;
import com.google.cloud.bigquery.BigQueryOptions;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Wires a BigQuery client. When {@code bigquery.emulator-endpoint} is set (tests), it points at the
 * goccy BigQuery emulator with no credentials; otherwise it uses application-default credentials.
 */
@Configuration
public class BigQueryConfiguration {

    @Bean
    public BigQuery bigQuery(
            @Value("${bigquery.project-id:test-project}") String projectId,
            @Value("${bigquery.emulator-endpoint:}") String emulatorEndpoint) throws Exception {
        BigQueryOptions.Builder builder = BigQueryOptions.newBuilder().setProjectId(projectId);
        if (emulatorEndpoint != null && !emulatorEndpoint.isBlank()) {
            builder.setHost(emulatorEndpoint).setLocation("US").setCredentials(NoCredentials.getInstance());
        } else {
            builder.setCredentials(GoogleCredentials.getApplicationDefault());
        }
        return builder.build().getService();
    }
}
