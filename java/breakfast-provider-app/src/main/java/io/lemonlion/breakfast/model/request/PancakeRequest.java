package io.lemonlion.breakfast.model.request;

import java.util.ArrayList;
import java.util.List;

/** Twin of C# {@code PancakeRequest}. */
public record PancakeRequest(String milk, String flour, String eggs, List<String> toppings) {

    public PancakeRequest {
        if (toppings == null) {
            toppings = new ArrayList<>();
        }
    }
}
