Feature: Specifications Grpc Reflection
    gRPC server reflection - Letting gRPC tools discover the breakfast service

    @happy-path
    Scenario: Grpc server reflection should list the breakfast service
        When the services are listed through grpc server reflection
        Then the breakfast service should be listed
        And the reflection service should be listed

    @happy-path
    Scenario: Grpc server reflection should describe the breakfast service
        When the breakfast service is described through grpc server reflection
        Then the description should be the breakfast proto file
        And the description should contain every breakfast method
