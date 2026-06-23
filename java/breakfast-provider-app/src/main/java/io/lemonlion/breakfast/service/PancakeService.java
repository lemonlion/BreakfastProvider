package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;

/** Twin of C# {@code IPancakeService}. */
public interface PancakeService {

    PancakeResponse makePancakes(PancakeRequest request);
}
