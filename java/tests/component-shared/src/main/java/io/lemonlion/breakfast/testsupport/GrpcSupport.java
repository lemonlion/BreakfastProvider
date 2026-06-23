package io.lemonlion.breakfast.testsupport;

import io.grpc.CallOptions;
import io.grpc.Channel;
import io.grpc.ClientCall;
import io.grpc.ClientInterceptor;
import io.grpc.ForwardingClientCall.SimpleForwardingClientCall;
import io.grpc.ManagedChannel;
import io.grpc.Metadata;
import io.grpc.MethodDescriptor;
import io.grpc.inprocess.InProcessChannelBuilder;
import io.kronikol.core.constants.TrackingHeaders;
import io.kronikol.core.context.TestIdentityScope;
import io.kronikol.core.context.TestInfo;
import io.kronikol.grpc.GrpcTracking.GrpcTrackingOptions;
import io.kronikol.grpc.KronikolClientInterceptor;
import io.lemonlion.breakfast.grpc.BreakfastGrpcGrpc;
import io.lemonlion.breakfast.grpc.BreakfastGrpcGrpc.BreakfastGrpcBlockingStub;

/**
 * Shared gRPC test support: an in-process channel to the SUT's gRPC server, wired with a Kronikol4J
 * client interceptor (records the call in the report) and an identity interceptor (propagates the
 * current test identity via metadata so the SUT attributes its Cosmos lookups to the test). Twin of
 * the C# {@code GrpcBreakfastSteps} / {@code CreateTestTrackingGrpcClient}.
 */
public final class GrpcSupport {

    /** In-process server name shared by the SUT (BackendsInitializer) and the test channel. */
    public static final String IN_PROCESS_NAME = "breakfast-grpc-test";

    private static final String SERVICE_NAME = "Breakfast Provider";

    private static final Metadata.Key<String> NAME_KEY =
            Metadata.Key.of(TrackingHeaders.CURRENT_TEST_NAME, Metadata.ASCII_STRING_MARSHALLER);
    private static final Metadata.Key<String> ID_KEY =
            Metadata.Key.of(TrackingHeaders.CURRENT_TEST_ID, Metadata.ASCII_STRING_MARSHALLER);

    private static volatile ManagedChannel channel;

    private GrpcSupport() {
    }

    /** A blocking stub over the shared, tracked in-process channel. */
    public static BreakfastGrpcBlockingStub blockingStub() {
        return BreakfastGrpcGrpc.newBlockingStub(channel());
    }

    private static ManagedChannel channel() {
        ManagedChannel local = channel;
        if (local == null) {
            synchronized (GrpcSupport.class) {
                local = channel;
                if (local == null) {
                    GrpcTrackingOptions options =
                            new GrpcTrackingOptions(SERVICE_NAME, "Test", TestIdentityScope::current);
                    local = InProcessChannelBuilder.forName(IN_PROCESS_NAME)
                            .intercept(new IdentityInterceptor(), new KronikolClientInterceptor(options))
                            .build();
                    channel = local;
                }
            }
        }
        return local;
    }

    /** Stamps the current test-identity headers onto outgoing gRPC metadata. */
    private static final class IdentityInterceptor implements ClientInterceptor {
        @Override
        public <ReqT, RespT> ClientCall<ReqT, RespT> interceptCall(
                MethodDescriptor<ReqT, RespT> method, CallOptions callOptions, Channel next) {
            return new SimpleForwardingClientCall<>(next.newCall(method, callOptions)) {
                @Override
                public void start(Listener<RespT> responseListener, Metadata headers) {
                    TestInfo identity = TestIdentityScope.current();
                    if (identity != null) {
                        headers.put(NAME_KEY, identity.name());
                        if (identity.id() != null) {
                            headers.put(ID_KEY, String.valueOf(identity.id()));
                        }
                    }
                    super.start(responseListener, headers);
                }
            };
        }
    }
}
