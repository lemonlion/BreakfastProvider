package io.lemonlion.breakfast.testsupport;

/**
 * Selects the component-test run mode from system properties (set by the Maven profile / surefire):
 *
 * <ul>
 *   <li><b>docker</b> (default): {@code external.sut.url} is empty — the test starts the SUT in-process
 *       via {@code @SpringBootTest} and provisions backends with Testcontainers.</li>
 *   <li><b>external-sut</b>: {@code external.sut.url} is set — the test should drive an already-deployed
 *       SUT over HTTP (and gRPC via {@code external.grpc.target}) instead of starting it in-process.</li>
 * </ul>
 *
 * <p>See {@code docs/RUN_MODES.md} for how the external-sut bases consume these (and how to verify the
 * mode against a real deployment, which cannot be exercised by the in-process docker-mode suite).
 */
public final class RunMode {

    private RunMode() {
    }

    public static boolean isExternalSut() {
        String url = System.getProperty("external.sut.url");
        return url != null && !url.isBlank();
    }

    /** External SUT base URL (HTTP), or {@code null} in docker mode. */
    public static String externalSutUrl() {
        String url = System.getProperty("external.sut.url");
        return (url == null || url.isBlank()) ? null : url;
    }

    /** External SUT gRPC target (host:port), or {@code null} in docker mode. */
    public static String externalGrpcTarget() {
        String target = System.getProperty("external.grpc.target");
        return (target == null || target.isBlank()) ? null : target;
    }
}
