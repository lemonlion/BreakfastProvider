# Component Tests — Skill Index

This directory contains conventions, patterns, and infrastructure guidance for the component test suite in `tests/BreakfastProvider.Tests.Component/`.

## Files

| File | Read this when… |
|---|---|
| [SKILL.md](SKILL.md) | **Always read.** Core rules, hard constraints, parallel safety, TDD workflow. |
| [naming-conventions.md](naming-conventions.md) | Creating or renaming feature classes, scenarios, or step methods. |
| [composite-patterns.md](composite-patterns.md) | Building or modifying `CompositeStep` methods (inner/outer, ingredient grouping, config-as-steps). |
| [assertion-patterns.md](assertion-patterns.md) | Adding any assertion — response, ingredient, topping, recipe validation, downstream. |
| [test-infrastructure.md](test-infrastructure.md) | Working on test modes (InMemory/Docker/Post-Deployment), fake services, post-deployment skip patterns (`[SkipStepIf]`, `[IgnoreIf]`, `IgnoreReasons`), report generation, or CI pipeline. |
