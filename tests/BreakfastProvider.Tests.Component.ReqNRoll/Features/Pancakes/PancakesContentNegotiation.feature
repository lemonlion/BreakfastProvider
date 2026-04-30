Feature: Pancakes Content Negotiation
    /pancakes - Content negotiation and unsupported media types

    Scenario Outline: Sending a request with an unsupported content type should return an unsupported media type response
        Given a pancake request with content type "<ContentType>"
        When the pancakes are prepared with the given content type
        Then the response should indicate unsupported media type

        Examples:
            | ContentType      |
            | text/plain       |
            | application/xml  |
            | text/html        |
