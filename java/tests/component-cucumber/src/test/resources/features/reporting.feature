Feature: Reporting
  GraphQL reporting over ingested order facts, mirrored from the C# HotChocolate ReportingQuery.

  Scenario: A created order appears in the GraphQL order summaries
    When an order is placed and the order summaries are queried via GraphQL
    Then the order appears in the reporting summaries

  Scenario: Popular recipes reflects the ordered recipe types
    When an order is placed and popular recipes are queried via GraphQL
    Then the popular recipes include "Pancakes"

  Scenario: An ingredient delivery posted to the EventGrid webhook appears in ingredient shipments
    When an ingredient delivery is posted to the EventGrid webhook
    Then the response status is 200
    And the ingredient shipment appears in the reporting shipments

  Scenario: A completed pancake batch is ingested into batch completions
    When a pancake batch is completed
    Then the batch appears in the batch completions

  Scenario: A batch's equipment alert flows through Event Hubs into equipment alerts
    When a pancake batch generates an equipment alert
    Then the equipment alert appears in the equipment alerts

  Scenario: A logged recipe is ingested into recipe reports via Kafka
    When a pancake recipe is logged for reporting
    Then the recipe report appears in the recipe reports

  Scenario: Ingredient usage aggregates across logged recipes
    When a pancake recipe is logged for ingredient usage
    Then the ingredient usage includes the logged ingredient
