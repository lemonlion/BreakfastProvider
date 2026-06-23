package io.lemonlion.breakfast.storage;

import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

/** Spring Data JPA repository for {@link StaffMemberEntity}. */
public interface StaffMemberRepository extends JpaRepository<StaffMemberEntity, Integer> {

    List<StaffMemberEntity> findAllByOrderByNameAsc();
}
