package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.testng.annotations.Test;

/** Staff domain component tests (TestNG). */
public class StaffTestNgTest extends ComponentTestBaseNg {

    private static StaffMemberRequest valid() {
        return new StaffMemberRequest("Sam Cook", "Chef", "sam@example.com", true, null);
    }

    @Test
    public void createAndRetrieve() {
        TestResponse created = client.post("/staff", valid());
        assertThat(created.status()).isEqualTo(201);
        StaffMemberResponse staff = created.as(StaffMemberResponse.class);
        assertThat(staff.id()).isPositive();
        assertThat(client.get("/staff/" + staff.id()).status()).isEqualTo(200);
    }

    @Test
    public void rejectsInvalidRole() {
        TestResponse response = client.post("/staff",
                new StaffMemberRequest("Sam", "Astronaut", "sam@example.com", true, null));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Role' must be one of:")).isTrue();
    }

    @Test
    public void rejectsInvalidEmail() {
        TestResponse response = client.post("/staff",
                new StaffMemberRequest("Sam", "Chef", "not-an-email", true, null));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Email' must be a valid email address.")).isTrue();
    }

    @Test
    public void deleteThenMissing() {
        StaffMemberResponse staff = client.post("/staff", valid()).as(StaffMemberResponse.class);
        assertThat(client.delete("/staff/" + staff.id()).status()).isEqualTo(204);
        assertThat(client.delete("/staff/" + staff.id()).status()).isEqualTo(404);
    }
}
