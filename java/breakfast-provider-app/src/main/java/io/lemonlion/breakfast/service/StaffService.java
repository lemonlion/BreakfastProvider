package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;
import java.util.List;
import java.util.Optional;

/** Twin of C# {@code IStaffService}. */
public interface StaffService {

    StaffMemberResponse create(StaffMemberRequest request);

    Optional<StaffMemberResponse> getById(int id);

    List<StaffMemberResponse> list();

    Optional<StaffMemberResponse> update(int id, StaffMemberRequest request);

    boolean delete(int id);
}
