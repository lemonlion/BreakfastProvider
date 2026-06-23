package io.lemonlion.breakfast.model.response;

import java.util.List;

/** Twin of C# {@code PaginatedResponse<T>}. */
public record PaginatedResponse<T>(
        List<T> items,
        int page,
        int pageSize,
        int totalCount,
        int totalPages) {
}
