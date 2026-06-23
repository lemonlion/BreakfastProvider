package io.lemonlion.breakfast.storage;

import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link OrderSummaryEntity}. */
public interface OrderSummaryRepository extends JpaRepository<OrderSummaryEntity, Integer> {

    Optional<OrderSummaryEntity> findByOrderId(UUID orderId);
}
