package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** CustomerPreferences domain component tests (JUnit 5) — Spanner persistence. */
@DisplayName("CustomerPreferences")
class CustomerPreferencesComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("a preference is upserted and then retrievable")
    void upsertAndRetrieve() {
        String customerId = "cust-" + UUID.randomUUID();
        CustomerPreferenceRequest request =
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes");

        TestResponse upserted = client.put("/customer-preferences/" + customerId, request);
        assertThat(upserted.status()).isEqualTo(200);
        CustomerPreferenceResponse pref = upserted.as(CustomerPreferenceResponse.class);
        assertThat(pref.customerId()).isEqualTo(customerId);
        assertThat(pref.preferredMilkType()).isEqualTo("oat");

        TestResponse fetched = client.get("/customer-preferences/" + customerId);
        assertThat(fetched.status()).isEqualTo(200);
        assertThat(fetched.as(CustomerPreferenceResponse.class).customerName()).isEqualTo("Alice");
    }

    @Test
    @DisplayName("retrieving an unknown customer returns 404")
    void getMissing() {
        assertThat(client.get("/customer-preferences/unknown-" + UUID.randomUUID()).status()).isEqualTo(404);
    }

    @Test
    @DisplayName("an empty customer name is rejected")
    void rejectsEmptyName() {
        TestResponse response = client.put("/customer-preferences/cust-x",
                new CustomerPreferenceRequest(null, "", "oat", false, ""));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Customer Name' must not be empty.")).isTrue();
    }
}
