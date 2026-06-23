package io.lemonlion.breakfast.model.response;

/** Twin of C# {@code EggsResponse}. */
public record EggsResponse(String eggs) {

    public EggsResponse() {
        this("Some_Eggs");
    }
}
