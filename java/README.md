# Breakfast Provider (Java)

A Java twin of the C# [BreakfastProvider](../) reference service. Its purpose is to **showcase
[Kronikol4J](https://github.com/lemonlion/Kronikol4J)** the same way the C# project showcases Kronikol:
run the same breakfast-domain component-test scenarios through **JUnit 5, TestNG, Cucumber and Spock**,
and publish a Kronikol4J HTML report (with PlantUML interaction diagrams) per framework to the shared
GitHub Pages site.

See the implementation plan and decisions in the repo's plan notes; this module follows a phased build.

## Prerequisites

- **JDK 17+** (validated on JDK 25).
- **Kronikol4J** is consumed from **Maven Central** as `io.github.lemonlion:kronikol4j-*:0.1.24` — no
  local checkout or `publishToMavenLocal` is needed; `./mvnw` resolves it automatically.

## Build & test

The Maven Wrapper bootstraps the right Maven version automatically — no global Maven needed:

```bash
./mvnw clean verify
```

Each component-test module emits Kronikol4J report fragments to `target/kronikol-fragments/`, then a
merge step (Kronikol4J CLI) combines them into `target/Reports/TestRunReport.html`.

## Module layout

```
java/
  pom.xml                      # parent reactor (Java 21, Kronikol4J versions, shared plugin config)
  tests/
    component-junit5/          # JUnit 5 suite        -> kronikol4j-junit5      (DONE: pipeline smoke)
    component-testng/          # TestNG suite         -> kronikol4j-testng      (planned)
    component-cucumber/        # Cucumber (Gherkin)   -> kronikol4j-cucumber    (planned)
    component-spock/           # Spock (Groovy)       -> kronikol4j-spock       (planned)
  breakfast-provider-app/      # the Spring Boot SUT (planned)
  fakes/                       # 5 downstream fake services (planned)
```

## Report pipeline (how it works)

1. Surefire runs each test JVM with `-Dkronikol.run.dir=target/kronikol-fragments` (set in the parent
   POM), so Kronikol4J's `LauncherSessionListener` writes a `fragment-<pid>.json` per fork.
2. A `verify`-phase step runs `io.kronikol.cli.Main merge <fragments> -o target/Reports/TestRunReport.html`
   to produce the final HTML report the GitHub Pages site links to.
