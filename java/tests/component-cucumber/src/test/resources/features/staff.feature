Feature: Staff
  Staff member management, mirrored from the C# Staff scenarios.

  Scenario: A staff member is added with a valid role
    When a staff member "Sam Cook" with role "Chef" is added
    Then the staff member is created with a role of "Chef"

  Scenario: A staff member with an invalid role is rejected
    When a staff member "Sam Cook" with role "Astronaut" is added
    Then the response status is 400
    And the error mentions "'Role' must be one of:"

  Scenario: An existing staff member is retrievable by id
    When a staff member is added and retrieved by id
    Then the retrieved staff member has id matching the created one

  Scenario: A staff member is deleted
    When a staff member is added and deleted
    Then the response status is 204
