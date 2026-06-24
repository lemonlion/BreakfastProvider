package io.lemonlion.breakfast.storage;

import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link EquipmentAlertEntity}. */
public interface EquipmentAlertRepository extends JpaRepository<EquipmentAlertEntity, Integer> {
}
