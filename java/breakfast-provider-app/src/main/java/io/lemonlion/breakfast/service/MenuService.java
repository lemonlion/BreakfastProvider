package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.response.MenuItemResponse;
import java.util.List;

/** Twin of C# {@code IMenuService}. */
public interface MenuService {

    List<MenuItemResponse> getMenu();

    void clearCache();
}
