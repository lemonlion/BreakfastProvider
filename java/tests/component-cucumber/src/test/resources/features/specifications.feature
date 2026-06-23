Feature: Specifications
  API specification documents mirrored from the C# Specifications scenarios: OpenAPI, Scalar UI and AsyncAPI.

  Scenario: The OpenAPI document contains all the endpoints
    When the OpenAPI document is requested
    Then the OpenAPI paths include all the breakfast endpoints

  Scenario: The Scalar UI endpoint returns a valid Scalar page
    When the Scalar UI is requested
    Then the response is a Scalar HTML page

  Scenario: The AsyncAPI document contains the expected sections
    When the AsyncAPI document is requested
    Then the AsyncAPI document contains the expected top-level sections
