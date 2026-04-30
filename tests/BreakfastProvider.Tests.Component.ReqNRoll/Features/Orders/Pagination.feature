Feature: Order Pagination
    /orders - Paginated listing of breakfast orders

    @IgnoreIfNeedsDirectDbAccess @happy-path
    Scenario: Listing orders should return a paginated response
        Given multiple orders have been created
        When orders are listed with default pagination
        Then the paginated response should contain the orders
        And the paginated response should have correct page metadata

    @IgnoreIfNeedsDirectDbAccess
    Scenario: Listing orders with a small page size should limit results
        Given multiple orders have been created
        When orders are listed with page 1 and page size 1
        Then the paginated response should have correct page metadata

    @IgnoreIfNeedsDirectDbAccess
    Scenario: Requesting the second page should return different orders
        Given multiple orders have been created
        When orders are listed with page 2 and page size 1
        Then the paginated response should have correct page metadata

    @IgnoreUnlessInMemoryDb
    Scenario: Listing orders when none exist should return an empty page
        When orders are listed with default pagination
        Then the paginated response should be empty
