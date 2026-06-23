package io.lemonlion.breakfast.model.response;

/** Twin of C# {@code FlourResponse}. */
public record FlourResponse(String flour) {

    public FlourResponse() {
        this("Some_Flour");
    }
}
