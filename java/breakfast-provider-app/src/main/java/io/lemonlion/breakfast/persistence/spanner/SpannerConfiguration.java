package io.lemonlion.breakfast.persistence.spanner;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.datasource.DriverManagerDataSource;

/**
 * A dedicated JDBC template for Google Spanner (separate from the primary SQL Server datasource).
 * The connection is lazy, so only the Spanner-backed domains actually open it.
 */
@Configuration
public class SpannerConfiguration {

    @Bean
    public JdbcTemplate spannerJdbcTemplate(
            @Value("${spanner.jdbc-url:jdbc:cloudspanner://localhost:9010/projects/test-project/instances/"
                    + "test-instance/databases/breakfast?autoConfigEmulator=true}") String url) {
        DriverManagerDataSource dataSource = new DriverManagerDataSource(url);
        dataSource.setDriverClassName("com.google.cloud.spanner.jdbc.JdbcDriver");
        return new JdbcTemplate(dataSource);
    }
}
