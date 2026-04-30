Feature: Waffles Content Negotiation
    /waffles - Content negotiation and unsupported media types

    Scenario Outline: Sending a request with an unsupported content type should return an unsupported media type response
        Given a waffle request with content type "<ContentType>"
        When the waffles are prepared with the given content type
        Then the waffle response should indicate unsupported media type

        Examples:
            | ContentType      |
            | text/plain       |
            | application/xml  |
            | text/html        |
