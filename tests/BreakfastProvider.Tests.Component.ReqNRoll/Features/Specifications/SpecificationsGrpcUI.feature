Feature: Specifications Grpc UI
    /grpc/ - Serving the gRPC documentation page

    @happy-path
    Scenario: The Grpc UI endpoint should return a page describing the service
        When the grpc ui endpoint is called
        Then the response should be a valid grpc documentation page
        And the page should describe every breakfast method
        And the page should link to the grpc contract
        And the grpc ui page is written to disk
