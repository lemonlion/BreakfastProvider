package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Staff domain component tests (JUnit 5) — relational persistence. */
@DisplayName("Staff")
class StaffComponentTest extends ComponentTestBase {

    private static StaffMemberRequest valid() {
        return new StaffMemberRequest("Sam Cook", "Chef", "sam@example.com", true, null);
    }

    @Test
    @DisplayName("a staff member is created and retrievable")
    void createAndRetrieve() {
        TestResponse created = client.post("/staff", valid());
        assertThat(created.status()).isEqualTo(201);
        StaffMemberResponse staff = created.as(StaffMemberResponse.class);
        assertThat(staff.id()).isPositive();
        assertThat(staff.role()).isEqualTo("Chef");
        assertThat(client.get("/staff/" + staff.id()).status()).isEqualTo(200);
    }

    @Test
    @DisplayName("an invalid role is rejected")
    void rejectsInvalidRole() {
        TestResponse response = client.post("/staff",
                new StaffMemberRequest("Sam", "Astronaut", "sam@example.com", true, null));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Role' must be one of:")).isTrue();
    }

    @Test
    @DisplayName("an invalid email is rejected")
    void rejectsInvalidEmail() {
        TestResponse response = client.post("/staff",
                new StaffMemberRequest("Sam", "Chef", "not-an-email", true, null));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Email' must be a valid email address.")).isTrue();
    }

    @Test
    @DisplayName("deleting a staff member returns 204 then 404")
    void deleteThenMissing() {
        StaffMemberResponse staff = client.post("/staff", valid()).as(StaffMemberResponse.class);
        assertThat(client.delete("/staff/" + staff.id()).status()).isEqualTo(204);
        assertThat(client.delete("/staff/" + staff.id()).status()).isEqualTo(404);
    }
}
