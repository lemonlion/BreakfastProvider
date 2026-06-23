package io.lemonlion.breakfast.grpc;

import net.devh.boot.grpc.server.interceptor.GrpcGlobalServerInterceptor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Registers the Kronikol4J identity-propagation interceptor on every gRPC server call. */
@Configuration
public class GrpcServerConfig {

    @GrpcGlobalServerInterceptor
    @Bean
    public KronikolGrpcServerInterceptor kronikolGrpcServerInterceptor() {
        return new KronikolGrpcServerInterceptor();
    }
}
