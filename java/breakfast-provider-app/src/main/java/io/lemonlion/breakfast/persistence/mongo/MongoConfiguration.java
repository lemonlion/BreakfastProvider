package io.lemonlion.breakfast.persistence.mongo;

import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Wires a MongoDB client (C# ChefNotes uses the Mongo driver directly). Connects lazily on first use. */
@Configuration
public class MongoConfiguration {

    @Bean(destroyMethod = "close")
    public MongoClient mongoClient(@Value("${mongodb.uri:mongodb://localhost:27017}") String uri) {
        return MongoClients.create(uri);
    }
}
