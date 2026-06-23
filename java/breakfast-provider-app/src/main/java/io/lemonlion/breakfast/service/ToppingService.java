package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

/** Twin of C# {@code IToppingService}. */
public interface ToppingService {

    List<ToppingResponse> getAvailableToppings();

    ToppingResponse createTopping(ToppingRequest request);

    Optional<ToppingResponse> updateTopping(UUID toppingId, UpdateToppingRequest request);

    boolean deleteTopping(UUID toppingId);
}
