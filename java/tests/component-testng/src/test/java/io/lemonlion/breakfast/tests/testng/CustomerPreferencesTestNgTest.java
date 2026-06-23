package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.UUID;
import org.testng.annotations.Test;

/** CustomerPreferences domain component tests (TestNG). */
public class CustomerPreferencesTestNgTest extends ComponentTestBaseNg {

    @Test
    public void upsertAndRetrieve() {
        String customerId = "cust-" + UUID.randomUUID();
        TestResponse upserted = client.put("/customer-preferences/" + customerId,
                new CustomerPreferenceRequest(null, "Alice", "oat", true, "Pancakes"));
        assertThat(upserted.status()).isEqualTo(200);
        assertThat(upserted.as(CustomerPreferenceResponse.class).preferredMilkType()).isEqualTo("oat");
        assertThat(client.get("/customer-preferences/" + customerId).status()).isEqualTo(200);
    }

    @Test
    public void getMissing() {
        assertThat(client.get("/customer-preferences/unknown-" + UUID.randomUUID()).status()).isEqualTo(404);
    }

    @Test
    public void rejectsEmptyName() {
        TestResponse response = client.put("/customer-preferences/cust-x",
                new CustomerPreferenceRequest(null, "", "oat", false, ""));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Customer Name' must not be empty.")).isTrue();
    }
}
