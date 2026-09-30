Feature: Specifications Grpc Contract
    /grpc/v1.json; /grpc/protos/breakfast.proto - Serving the gRPC contract as a descriptor set and as its proto file

    @happy-path
    Scenario: The Grpc contract endpoint should return a valid specification
        When the grpc contract endpoint is called
        Then the response should be valid
        And the grpc contract should describe the breakfast service
        And every breakfast method should be documented
        And the grpc contract is written to disk

    @happy-path
    Scenario: The Grpc proto endpoint should return the proto file
        When the grpc proto endpoint is called
        Then the response should be a plain text proto file
        And the proto file should declare the breakfast service
