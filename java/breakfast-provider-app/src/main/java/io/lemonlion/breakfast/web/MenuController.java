package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.response.MenuItemResponse;
import io.lemonlion.breakfast.service.MenuService;
import java.util.List;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code MenuController} ({@code /menu}). */
@RestController
@RequestMapping(path = "/menu", produces = MediaType.APPLICATION_JSON_VALUE)
public class MenuController {

    private final MenuService menuService;

    public MenuController(MenuService menuService) {
        this.menuService = menuService;
    }

    @GetMapping
    public List<MenuItemResponse> getMenu() {
        return menuService.getMenu();
    }

    @DeleteMapping("/cache")
    public ResponseEntity<Void> clearCache() {
        menuService.clearCache();
        return ResponseEntity.noContent().build();
    }
}
