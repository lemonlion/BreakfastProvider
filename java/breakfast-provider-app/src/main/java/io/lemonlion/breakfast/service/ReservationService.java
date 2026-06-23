package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.ReservationRequest;
import io.lemonlion.breakfast.model.response.ReservationResponse;
import java.util.List;
import java.util.Optional;

/** Twin of C# {@code IReservationService}. */
public interface ReservationService {

    ReservationResponse create(ReservationRequest request);

    Optional<ReservationResponse> getById(int id);

    List<ReservationResponse> list();

    /** Empty if the reservation does not exist (or is cancelled, which the C# treats as not-updatable). */
    Optional<ReservationResponse> update(int id, ReservationRequest request);

    CancelResult cancel(int id);

    boolean delete(int id);

    /** Outcome of {@link #cancel}: not-found, an error (already cancelled), or the cancelled reservation. */
    record CancelResult(ReservationResponse reservation, String error, boolean notFound) {

        public static CancelResult ok(ReservationResponse reservation) {
            return new CancelResult(reservation, null, false);
        }

        public static CancelResult error(String error) {
            return new CancelResult(null, error, false);
        }

        public static CancelResult notFoundResult() {
            return new CancelResult(null, null, true);
        }
    }
}
