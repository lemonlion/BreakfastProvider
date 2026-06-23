package io.lemonlion.breakfast.persistence;

import java.util.List;

/** A page of repository results plus the total matching count (twin of C# {@code (Items, TotalCount)}). */
public record PagedItems<T>(List<T> items, int totalCount) {
}
