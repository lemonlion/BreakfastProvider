package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

/** Twin of C# {@code IDailySpecialsService}. */
public interface DailySpecialsService {

    List<DailySpecialResponse> getAvailableSpecials();

    Optional<DailySpecialOrderResponse> checkIdempotency(String idempotencyKey);

    Optional<Special> validateSpecialExists(UUID specialId);

    /** Reserves quantity; empty if the special is sold out for today. */
    Optional<DailySpecialOrderResponse> reserveQuantity(UUID specialId, int quantity, String specialName);

    void storeIdempotencyResult(String idempotencyKey, DailySpecialOrderResponse response);

    void publishOrderEvent(DailySpecialOrderResponse response, String specialName);

    void resetOrderCounts(UUID specialId);

    /** A daily special's catalogue entry. */
    record Special(UUID id, String name, String description) {
    }
}
