package io.lemonlion.breakfast.testsupport;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;

/** Shared Jackson mapper configured like the SUT (JSR-310 instants, lenient on unknown fields). */
public final class JsonMappers {

    private static final ObjectMapper INSTANCE = new ObjectMapper()
            .registerModule(new JavaTimeModule())
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);

    private JsonMappers() {
    }

    public static ObjectMapper instance() {
        return INSTANCE;
    }
}
