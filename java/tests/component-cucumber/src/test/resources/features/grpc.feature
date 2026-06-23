Feature: Grpc
  gRPC recipe summaries and order status lookups, mirrored from the C# BreakfastGrpcService.

  Scenario: Recipe summary for pancakes
    When a recipe summary is requested for "Pancakes" via grpc
    Then the recipe summary has 42 total batches
    And the common ingredients are "Milk,Flour,Eggs"

  Scenario: Recipe summary for waffles
    When a recipe summary is requested for "Waffles" via grpc
    Then the recipe summary has 28 total batches
    And the common ingredients are "Milk,Flour,Eggs,Butter"

  Scenario: Recipe summary for an unknown type
    When a recipe summary is requested for "Unknown" via grpc
    Then the recipe summary has 0 total batches
    And the common ingredients are empty

  Scenario: Order status for a created order
    When an order is placed and its status is requested via grpc
    Then the grpc order status is "Created"

  Scenario: Order status for a non-existent order
    When the status of a non-existent order is requested via grpc
    Then the grpc response is a not found error

  Scenario: Stream order updates for a created order
    When an order is placed and its updates are streamed via grpc
    Then the streamed order status is "Created"
