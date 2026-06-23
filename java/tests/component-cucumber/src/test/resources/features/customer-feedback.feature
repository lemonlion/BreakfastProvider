Feature: Customer Feedback Alert Processing
  Pub/Sub consumer: a published feedback event triggers downstream processing (Mongo + notify + supplier),
  mirrored from the C# CustomerFeedback scenario.

  Scenario: Consuming a customer feedback event triggers downstream processing
    When a customer feedback event is published
    Then the supplier service is notified of the feedback
