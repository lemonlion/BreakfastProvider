Feature: Muffins Creation
    /muffins - Creating apple cinnamon muffins with baking profiles and toppings

    @happy-path
    Scenario: A valid apple cinnamon muffin request should return a fresh batch
        Given a valid apple cinnamon muffin recipe with all ingredients
        When the muffins are prepared
        Then the muffin response should contain a valid batch with all ingredients
        And the cow service should have received a milk request for the muffins

    Scenario Outline: Different muffin recipes should produce the expected batch
        Given a muffin recipe "<RecipeName>" with the following ingredients:
            | Flour   | Apples         | Cinnamon       |
            | <Flour> | <AppleVariety> | <CinnamonType> |
        And with baking at <Temperature> degrees for <Duration> minutes in a "<PanType>" pan
        And the following muffin toppings:
            | Name       | Amount    |
            | <Topping1> | <Amount1> |
            | <Topping2> | <Amount2> |
        When the muffins are prepared
        Then the muffin batch should have <ExpectedIngredientCount> ingredients
        And the muffin response should include <ExpectedToppingCount> toppings

        Examples:
            | RecipeName       | Flour       | AppleVariety | CinnamonType | Temperature | Duration | PanType   | Topping1          | Amount1 | Topping2           | Amount2 | ExpectedIngredientCount | ExpectedToppingCount |
            | Classic          | Plain Flour | Granny Smith | Ceylon       | 180         | 25       | Standard  | Streusel          | Light   | Icing Glaze        | Drizzle | 5                       | 2                    |
            | Rustic Wholesome | Whole Wheat | Honeycrisp   | Cassia       | 175         | 30       | Cast Iron | Brown Sugar Crumb | Heavy   | Maple Drizzle      | Light   | 5                       | 2                    |
            | Spiced Deluxe    | Almond      | Pink Lady    | Saigon       | 190         | 20       | Silicone  | Cinnamon Sugar    | Heavy   | Cream Cheese Swirl | Thick   | 5                       | 2                    |

    Scenario Outline: Muffins endpoint called with an invalid field should return a bad request response
        Given a valid muffin request with "<Field>" set to "<Value>"
        When the invalid muffin request is submitted
        Then the muffin response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field    | Value                          | Reason              | Error Message                                       | Response Status |
            | Flour    |                                | Flour is required   | 'Flour' is required.                                | Bad Request     |
            | Apples   |                                | Apples is required  | 'Apples' is required.                               | Bad Request     |
            | Cinnamon |                                | Cinnamon is required| 'Cinnamon' is required.                             | Bad Request     |
            | Milk     |                                | Milk is required    | 'Milk' is required.                                 | Bad Request     |
            | Eggs     |                                | Eggs is required    | 'Eggs' is required.                                 | Bad Request     |
            | Cinnamon | <script>alert('xss')</script>  | XSS in cinnamon     | Cinnamon contains potentially dangerous content.    | Bad Request     |
