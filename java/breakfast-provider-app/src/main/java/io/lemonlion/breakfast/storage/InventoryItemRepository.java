package io.lemonlion.breakfast.storage;

import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link InventoryItemEntity}. */
public interface InventoryItemRepository extends JpaRepository<InventoryItemEntity, Integer> {

    List<InventoryItemEntity> findAllByOrderByNameAsc();
}
