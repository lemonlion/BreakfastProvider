package io.lemonlion.breakfast.storage;

import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link BatchCompletionRecordEntity}. */
public interface BatchCompletionRecordRepository extends JpaRepository<BatchCompletionRecordEntity, Integer> {
}
