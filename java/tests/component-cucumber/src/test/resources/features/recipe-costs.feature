Feature: Recipe Costs
  Kafka consumer: a published recipe-cost event triggers BigQuery storage + notify + kitchen,
  mirrored from the C# RecipeCosts scenario.

  Scenario: Consuming a recipe-cost event notifies the kitchen
    When a recipe-cost calculated event is published
    Then the kitchen is notified of the recipe cost
