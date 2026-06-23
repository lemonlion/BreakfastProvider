package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.response.GoatMilkResponse;
import io.lemonlion.breakfast.model.response.MilkResponse;

/** Twin of C# {@code IMilkSourcingService}: sources milk from the Cow/Goat downstream services over HTTP. */
public interface MilkSourcingService {

    MilkResponse sourceFromCow();

    GoatMilkResponse sourceFromGoat();
}
