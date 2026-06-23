Feature: Reporting
  GraphQL reporting over ingested order facts, mirrored from the C# HotChocolate ReportingQuery.

  Scenario: A created order appears in the GraphQL order summaries
    When an order is placed and the order summaries are queried via GraphQL
    Then the order appears in the reporting summaries
