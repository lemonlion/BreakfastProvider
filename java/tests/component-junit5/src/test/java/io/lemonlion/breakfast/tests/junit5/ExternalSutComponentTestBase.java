package io.lemonlion.breakfast.tests.junit5;

import io.kronikol.junit5.KronikolExtension;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.RunMode;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.junit.jupiter.api.extension.ExtendWith;

/**
 * Base for <b>external-sut</b> mode component tests: drives an already-deployed SUT over HTTP (and gRPC via
 * {@link io.lemonlion.breakfast.testsupport.GrpcSupport}, which targets {@code external.grpc.target} in this
 * mode). Unlike {@link ComponentTestBase} it uses no {@code @SpringBootTest} and no Testcontainers — the SUT
 * and its backends are provisioned externally.
 *
 * <p>Enabled only when {@code -Dexternal.sut.url} is set (the {@code external-sut} Maven profile), so it is a
 * no-op in the default docker run. This mode cannot be exercised on a workstation without a deployment;
 * see {@code docs/RUN_MODES.md} for how to run/verify it (CI lane, or a local {@code docker run} of the
 * backends + the SUT exec jar). The in-JVM-fake assertions used by the docker-mode scenarios
 * ({@code BreakfastBackends.kitchen()} etc.) don't apply here, so external-sut scenarios assert only on the
 * SUT's own HTTP/gRPC responses.
 */
@EnabledIfSystemProperty(named = "external.sut.url", matches = ".+")
@ExtendWith(KronikolExtension.class)
public abstract class ExternalSutComponentTestBase {

    protected BreakfastTestClient client;

    @BeforeEach
    void initClient() {
        client = new BreakfastTestClient(RunMode.externalSutUrl());
    }
}
