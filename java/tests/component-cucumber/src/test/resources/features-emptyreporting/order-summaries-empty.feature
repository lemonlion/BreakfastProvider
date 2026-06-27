Feature: Order Summaries (empty)
  Reporting order-summaries empty-list behaviour, mirrored from the C# Reporting scenario. Runs against an
  isolated empty reporting store so no orders are present.

  Scenario: Order summaries return an empty list when no orders exist
    When the order summaries are queried with no orders present
    Then the order summaries list is empty
