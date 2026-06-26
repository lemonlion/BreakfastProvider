package io.lemonlion.breakfast.storage;

import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link RecipeReportEntity}. */
public interface RecipeReportRepository extends JpaRepository<RecipeReportEntity, Integer> {
}
