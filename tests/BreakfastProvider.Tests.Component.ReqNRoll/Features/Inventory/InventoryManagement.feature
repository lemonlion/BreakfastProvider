Feature: Inventory Management
    /inventory - Managing ingredient inventory with full CRUD operations

    @happy-path
    Scenario: Adding a new inventory item should return the created item
        Given a valid inventory item request
        When the inventory item is submitted
        Then the inventory response should contain the created item

    Scenario: Retrieving an existing inventory item should return the item
        Given an inventory item exists
        When the inventory item is retrieved by id
        Then the inventory get response should contain the item

    Scenario: Listing all inventory items should return all items
        Given an inventory item exists
        When all inventory items are requested
        Then the inventory list response should contain the item

    Scenario: Updating an inventory item should return the updated item
        Given an inventory item exists
        When the inventory item is updated
        Then the inventory update response should contain the updated values

    Scenario: Deleting an inventory item should return no content
        Given an inventory item exists
        When the inventory item is deleted
        Then the inventory delete response should indicate no content

    Scenario: Retrieving a non-existent inventory item should return not found
        When a non-existent inventory item is retrieved
        Then the inventory get response should indicate not found
