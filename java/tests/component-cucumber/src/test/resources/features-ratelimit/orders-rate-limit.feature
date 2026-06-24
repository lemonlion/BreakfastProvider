Feature: Orders rate limiting
  Fixed-window rate limiting on order creation, mirrored from the C# Orders Rate_Limiting scenario.
  Runs in its own Spring context with the permit limit overridden to 1.

  Scenario: A second order within the window is rate limited
    When two orders are placed within the rate-limit window
    Then the first order succeeds and the second is rate limited
