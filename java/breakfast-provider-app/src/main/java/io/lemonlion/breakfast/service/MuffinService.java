package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.response.MuffinResponse;

/** Twin of C# {@code IMuffinService}. */
public interface MuffinService {

    MuffinResponse makeMuffins(MuffinRequest request);
}
