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
