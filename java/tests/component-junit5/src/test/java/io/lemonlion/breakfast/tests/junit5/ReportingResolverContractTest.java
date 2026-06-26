package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import io.lemonlion.breakfast.reporting.ReportingGraphQlController;
import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/**
 * Plain (no-Spring) contract test for the C# {@code Order_Summaries_Should_Return_An_Empty_List_When_No
 * _Orders_Exist} scenario. That scenario can't run as a shared-store component test — the docker suite's
 * reporting store (shared MSSQL) always has orders by the time any test runs — so this verifies the
 * resolver's empty-collection contract directly: an empty store yields an empty (non-null) list. See
 * docs/REMAINING_PARITY.md for the full-transport verification plan (isolated/external-sut store).
 */
@DisplayName("Reporting resolver contract")
class ReportingResolverContractTest {

    @Test
    @DisplayName("orderSummaries returns an empty list when the reporting store is empty")
    void orderSummariesEmptyWhenStoreEmpty() {
        OrderSummaryRepository emptyRepo = mock(OrderSummaryRepository.class);
        when(emptyRepo.findAll()).thenReturn(List.of());

        ReportingGraphQlController controller =
                new ReportingGraphQlController(emptyRepo, null, null, null, null);

        assertThat(controller.orderSummaries()).isEmpty();
    }
}
