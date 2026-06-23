package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import java.util.List;
import java.util.Optional;

/** Twin of C# {@code IFeedbackService} (Spanner-backed). */
public interface FeedbackService {

    FeedbackResponse create(FeedbackRequest request);

    Optional<FeedbackResponse> getById(String feedbackId);

    List<FeedbackResponse> listByOrder(String orderId);
}
