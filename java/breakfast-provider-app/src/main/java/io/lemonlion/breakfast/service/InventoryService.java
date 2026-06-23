package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import io.lemonlion.breakfast.model.response.InventoryItemResponse;
import java.util.List;
import java.util.Optional;

/** Twin of C# {@code IInventoryService}. */
public interface InventoryService {

    InventoryItemResponse create(InventoryItemRequest request);

    Optional<InventoryItemResponse> getById(int id);

    List<InventoryItemResponse> list();

    Optional<InventoryItemResponse> update(int id, InventoryItemRequest request);

    boolean delete(int id);
}
