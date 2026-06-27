package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.EmptyReportingBackendsInitializer;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.Map;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.testng.AbstractTestNGSpringContextTests;
import org.testng.annotations.Test;

/** Order-summaries-empty scenario (TestNG): isolated empty H2 reporting store, no orders -> empty list. */
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = EmptyReportingBackendsInitializer.class)
public class ReportingEmptyTestNgTest extends AbstractTestNGSpringContextTests {

    @LocalServerPort
    int port;

    @Test
    public void orderSummariesEmptyWhenNoOrders() {
        BreakfastTestClient client = new BreakfastTestClient("http://127.0.0.1:" + port);

        TestResponse gql = client.post("/graphql", Map.of("query", "{ orderSummaries { orderId } }"));

        assertThat(gql.status()).isEqualTo(200);
        JsonNode summaries = gql.json().get("data").get("orderSummaries");
        assertThat(summaries.isArray()).isTrue();
        assertThat(summaries.size()).isZero();
    }
}
