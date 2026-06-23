package io.lemonlion.breakfast.model.request;

/** Twin of C# {@code CustomerPreferenceRequest}. */
public record CustomerPreferenceRequest(
        String customerId,
        String customerName,
        String preferredMilkType,
        boolean likesExtraToppings,
        String favouriteItem) {

    /** Returns a copy with the customer id set from the path (C# does {@code request with { CustomerId = ... }}). */
    public CustomerPreferenceRequest withCustomerId(String id) {
        return new CustomerPreferenceRequest(id, customerName, preferredMilkType, likesExtraToppings, favouriteItem);
    }
}
