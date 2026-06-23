package io.lemonlion.breakfast.infra;

import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Twin of the C# Scalar API reference UI ({@code Scalar.AspNetCore}). Serves a minimal Scalar page that
 * renders the OpenAPI document at {@code /openapi/v1.json}.
 */
@RestController
public class ScalarController {

    @GetMapping(value = "/scalar/v1", produces = MediaType.TEXT_HTML_VALUE)
    public String scalar() {
        return """
                <!doctype html>
                <html>
                  <head>
                    <title>Breakfast Provider API Reference</title>
                    <meta charset="utf-8"/>
                    <meta name="viewport" content="width=device-width, initial-scale=1"/>
                  </head>
                  <body>
                    <script id="api-reference" data-url="/openapi/v1.json"></script>
                    <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
                  </body>
                </html>
                """;
    }
}
