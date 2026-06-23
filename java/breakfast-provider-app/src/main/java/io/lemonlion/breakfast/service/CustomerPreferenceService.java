package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse;
import java.util.Optional;

/** Twin of C# {@code ICustomerPreferenceService} (Spanner-backed). */
public interface CustomerPreferenceService {

    CustomerPreferenceResponse upsert(CustomerPreferenceRequest request);

    Optional<CustomerPreferenceResponse> getById(String customerId);
}
