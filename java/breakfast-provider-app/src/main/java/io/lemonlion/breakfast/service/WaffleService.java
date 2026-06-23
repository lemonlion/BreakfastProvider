package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.WaffleRequest;
import io.lemonlion.breakfast.model.response.WaffleResponse;

/** Twin of C# {@code IWaffleService}. */
public interface WaffleService {

    WaffleResponse makeWaffles(WaffleRequest request);
}
