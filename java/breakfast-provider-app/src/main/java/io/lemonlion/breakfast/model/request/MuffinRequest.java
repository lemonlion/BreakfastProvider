package io.lemonlion.breakfast.model.request;

import java.util.List;

/** Twin of C# {@code MuffinRequest}. */
public record MuffinRequest(
        String milk,
        String flour,
        String eggs,
        String apples,
        String cinnamon,
        BakingProfile baking,
        List<MuffinTopping> toppings) {
}
