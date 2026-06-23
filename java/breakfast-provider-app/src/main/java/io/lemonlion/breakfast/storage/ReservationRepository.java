package io.lemonlion.breakfast.storage;

import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link ReservationEntity}. */
public interface ReservationRepository extends JpaRepository<ReservationEntity, Integer> {

    List<ReservationEntity> findAllByOrderByReservedAtAsc();
}
