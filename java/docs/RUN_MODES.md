# Java twin — run modes & verification plan

This document covers the two run modes of the Java twin's component-test suites and a detailed plan to
verify the parts that **cannot be exercised by the local in-process (docker) suite**: the `external-sut`
run mode and the GitHub Actions CI lanes.

## Run modes (twin of the C# memory / docker / external-sut lanes)

The C# project runs three lanes. The Java twin deliberately ships **two**, because Kronikol4J has no
in-memory emulator equivalents for the tracked backends (Cosmos, Spanner, BigQuery, Pub/Sub, …), so the
`memory` lane has no Java counterpart (a decision recorded with the user):

| Mode | How it runs | Backends | Local? |
|------|-------------|----------|--------|
| **docker** (default) | `@SpringBootTest(webEnvironment=RANDOM_PORT)` starts the SUT in-process | Testcontainers (Cosmos emulator, Kafka, SQL Server, Mongo, Spanner emulator, BigQuery emulator, Pub/Sub emulator) + in-JVM HTTP fakes | ✅ fully verified locally |
| **external-sut** | Tests drive an **already-deployed** SUT over HTTP/gRPC | Real, deployed backends (not provisioned by the test) | ⚠️ requires a deployment — see below |

### docker mode (default — verified)

```
cd java
JAVA_HOME=<jdk-21> DOCKER_API_VERSION=1.43 ./mvnw clean verify
```

Runs all four framework modules; each emits `tests/component-<fw>/target/Reports/TestRunReport.html`.
This is green: JUnit5 / TestNG / Cucumber / Spock all pass against the Testcontainers backends.

### external-sut mode

Activated by the `external-sut` Maven profile, which sets two system properties consumed by
`io.lemonlion.breakfast.testsupport.RunMode`:

```
./mvnw -Pexternal-sut \
       -Dexternal.sut.url=https://sut.example.com \
       -Dexternal.grpc.target=sut.example.com:9090 \
       -pl tests/component-junit5 test
```

`RunMode.isExternalSut()` / `externalSutUrl()` / `externalGrpcTarget()` expose the target.

**Remaining work for full external-sut support (not done — design + verification plan):**

**Foundation — built and docker-verified:**

1. `GrpcSupport` is RunMode-aware: in external-sut mode it dials a TCP channel at
   `RunMode.externalGrpcTarget()` (with the same Kronikol4J + identity interceptors); in docker mode it
   keeps the in-process channel. Verified green in docker mode.
2. `ExternalSutComponentTestBase` (JUnit5, **no** `@SpringBootTest`, no Testcontainers) builds
   `new BreakfastTestClient(RunMode.externalSutUrl())`. It is `@EnabledIfSystemProperty(external.sut.url)`
   so it is a no-op in the default docker run and activates only under the `external-sut` profile.
3. `ExternalSutSmokeTest` extends it: a representative HTTP/gRPC suite (health, menu, order create +
   retrieve, gRPC recipe summary) that asserts only on the SUT's own responses — no in-JVM-fake checks —
   proving the external transport path end-to-end against a deployment.

**Remaining (incremental) work — bring the *full* scenario suite to external mode:**

The ~150 docker-mode scenarios extend the `@SpringBootTest` bases and many assert on in-JVM fakes
(`BreakfastBackends.kitchen()`/`cow()`/…), which don't exist against a remote SUT. To run them all in
external mode, migrate each framework's scenarios onto an external base (like the JUnit5 one above) and,
for the fake-dependent assertions, either drop them in external mode or have the deployed fakes expose
query endpoints. This is a mechanical per-scenario migration; the transport foundation it builds on is in
place. It can only be validated against a real deployment (below), not on a workstation.

**How to verify external-sut locally** (end-to-end, before relying on CI):

1. Start the backends standalone (the same Testcontainers images via `docker run`, or a compose file):
   Cosmos emulator, Kafka, SQL Server, Mongo, Spanner emulator, BigQuery emulator, Pub/Sub emulator,
   plus the four HTTP fakes (or a deployed fakes set).
2. Run the SUT as a standalone process pointed at those backends:
   `java -jar breakfast-provider-app/target/breakfast-provider-app-1.0.0-SNAPSHOT-exec.jar`
   with `cosmos.*`, `spring.kafka.*`, `spring.datasource.*`, `mongodb.uri`, `spanner.*`, `bigquery.*`,
   `pubsub.*`, `downstream.*`, `grpc.server.port=9090` set to the standalone backends/fakes.
3. Run a framework module in external mode:
   `./mvnw -Pexternal-sut -Dexternal.sut.url=http://localhost:8080 -Dexternal.grpc.target=localhost:9090 -pl tests/component-junit5 test`
4. Confirm the scenarios pass against the out-of-process SUT and the report renders.

## CI verification (GitHub Actions — not locally runnable)

`.github/workflows/_tests-java.yml` + the four `java-*` jobs in `ci-main.yml` run the docker-mode suite
on GitHub-hosted Ubuntu runners and publish each framework's `TestRunReport.html` to the shared Pages
site. This cannot be exercised from this machine. To verify:

1. **Push the `java-twin` branch** (and merge to `main`, since `ci-main.yml` triggers on push to `main`).
2. Kronikol4J is resolved from **Maven Central** (`io.github.lemonlion:kronikol4j-*`, pinned by
   `<kronikol4j.version>` in `java/pom.xml`, currently `0.1.24`) — the `_tests-java.yml` workflow no
   longer clones Kronikol4J or runs `publishToMavenLocal`, and no `ORGANISATION_PAT` secret is needed for
   the Java lanes. To move to a newer Kronikol4J, bump that one property.
3. Confirm each `java-<fw>` job is green. **Risk:** the docker-mode suite needs eight backend containers
   incl. the heavy Cosmos emulator (~ multi-GB). Standard GitHub-hosted runners may be memory/time
   constrained; if a job flakes, options are a larger runner, container reuse, or splitting the suite.
4. Confirm the `deploy-pages` job downloads `java-<fw>-report` artifacts into `site/reports/java-<fw>/`
   and the published landing page shows the four "Java Test Run Reports (Kronikol4J)" cards linking to
   working reports, plus the Kronikol4J source card.

## Status summary

- **docker mode:** complete and green locally for all four frameworks (full `mvn verify`).
- **external-sut:** Maven profile + `RunMode` seam in place; the parallel non-`@SpringBootTest` bases are
  the remaining work, with the local + CI verification steps above.
- **CI/Pages:** authored and YAML-valid; verify by pushing the branch per the steps above.
