package io.lemonlion.breakfast.storage;

import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link IngredientShipmentEntity}. */
public interface IngredientShipmentRepository extends JpaRepository<IngredientShipmentEntity, Integer> {
}
