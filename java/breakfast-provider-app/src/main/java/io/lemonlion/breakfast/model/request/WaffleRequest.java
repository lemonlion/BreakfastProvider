package io.lemonlion.breakfast.model.request;

import java.util.ArrayList;
import java.util.List;

/** Twin of C# {@code WaffleRequest}. */
public record WaffleRequest(String milk, String flour, String eggs, String butter, List<String> toppings) {

    public WaffleRequest {
        if (toppings == null) {
            toppings = new ArrayList<>();
        }
    }
}
