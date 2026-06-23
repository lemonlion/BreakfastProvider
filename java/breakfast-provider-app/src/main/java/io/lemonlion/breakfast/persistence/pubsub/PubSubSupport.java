package io.lemonlion.breakfast.persistence.pubsub;

import com.google.api.gax.core.CredentialsProvider;
import com.google.api.gax.core.NoCredentialsProvider;
import com.google.api.gax.grpc.GrpcTransportChannel;
import com.google.api.gax.rpc.FixedTransportChannelProvider;
import com.google.api.gax.rpc.TransportChannelProvider;
import com.google.cloud.pubsub.v1.SubscriptionAdminSettings;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;
import io.lemonlion.breakfast.config.PubSubConfig;
import jakarta.annotation.PreDestroy;
import org.springframework.stereotype.Component;

/**
 * Provides the gax transport channel + credentials for Pub/Sub clients. Against the emulator it uses a
 * plaintext channel to {@code pubsub.emulator-endpoint} with no credentials; otherwise the gax defaults.
 */
@Component
public class PubSubSupport {

    private final PubSubConfig config;
    private ManagedChannel channel;

    public PubSubSupport(PubSubConfig config) {
        this.config = config;
    }

    public boolean usingEmulator() {
        return !config.getEmulatorEndpoint().isBlank();
    }

    public synchronized TransportChannelProvider channelProvider() {
        if (usingEmulator()) {
            if (channel == null) {
                channel = ManagedChannelBuilder.forTarget(config.getEmulatorEndpoint()).usePlaintext().build();
            }
            return FixedTransportChannelProvider.create(GrpcTransportChannel.create(channel));
        }
        try {
            return SubscriptionAdminSettings.defaultTransportChannelProvider();
        } catch (Exception e) {
            throw new IllegalStateException("Failed to build Pub/Sub transport channel provider", e);
        }
    }

    public CredentialsProvider credentialsProvider() {
        if (usingEmulator()) {
            return NoCredentialsProvider.create();
        }
        try {
            return SubscriptionAdminSettings.defaultCredentialsProviderBuilder().build();
        } catch (Exception e) {
            throw new IllegalStateException("Failed to build Pub/Sub credentials provider", e);
        }
    }

    @PreDestroy
    public synchronized void shutdown() {
        if (channel != null) {
            channel.shutdownNow();
        }
    }
}
