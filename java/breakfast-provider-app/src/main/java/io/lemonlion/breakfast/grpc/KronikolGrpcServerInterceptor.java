package io.lemonlion.breakfast.grpc;

import io.grpc.ForwardingServerCallListener.SimpleForwardingServerCallListener;
import io.grpc.Metadata;
import io.grpc.ServerCall;
import io.grpc.ServerCallHandler;
import io.grpc.ServerInterceptor;
import io.kronikol.core.constants.TrackingHeaders;
import io.kronikol.core.context.TestIdentityScope;

/**
 * gRPC server counterpart of the Kronikol4J servlet filter (Layer 1): reads the test-identity headers
 * from the call metadata and opens a {@link TestIdentityScope} for the duration of the RPC handler so
 * the SUT's downstream work (Cosmos lookups) attributes to the running test.
 *
 * <p>For unary and server-streaming RPCs the handler executes on the gRPC thread during
 * {@code onHalfClose}, so the scope is established there and cleared in a {@code finally}.
 */
public final class KronikolGrpcServerInterceptor implements ServerInterceptor {

    private static final Metadata.Key<String> NAME_KEY =
            Metadata.Key.of(TrackingHeaders.CURRENT_TEST_NAME, Metadata.ASCII_STRING_MARSHALLER);
    private static final Metadata.Key<String> ID_KEY =
            Metadata.Key.of(TrackingHeaders.CURRENT_TEST_ID, Metadata.ASCII_STRING_MARSHALLER);

    @Override
    public <ReqT, RespT> ServerCall.Listener<ReqT> interceptCall(
            ServerCall<ReqT, RespT> call, Metadata headers, ServerCallHandler<ReqT, RespT> next) {

        String testName = headers.get(NAME_KEY);
        if (testName == null) {
            return next.startCall(call, headers);
        }
        String testId = headers.get(ID_KEY);

        return new SimpleForwardingServerCallListener<>(next.startCall(call, headers)) {
            @Override
            public void onHalfClose() {
                TestIdentityScope.setFromMessage(testName, testId);
                try {
                    super.onHalfClose();
                } finally {
                    TestIdentityScope.clear();
                }
            }
        };
    }
}
