package io.lemonlion.breakfast.downstream;

/** Twin of the C# Supplier Service HTTP integration. */
public interface SupplierClient {

    /** @return true if the supplier confirms milk availability; false on any non-success or transport error. */
    boolean isMilkAvailable();
}
