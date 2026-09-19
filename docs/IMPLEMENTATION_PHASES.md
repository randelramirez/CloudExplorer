# Implementation phases

Every phase has a dedicated prompt under `automation/phase-prompts`. The runner gives each phase a fresh ephemeral session and durable repository context.

1. **Foundation** — Uno Skia Desktop scaffold, requirements, architecture, DI, and test project.
2. **Core services** — safe process execution, common resource model, nullable date semantics, and parsers.
3. **AWS** — profile selection, authentication, Resource Explorer query, type/tag grouping, and manual refresh.
4. **Azure** — subscription selection, authentication, resource listing, resource-group/type grouping, and manual refresh.
5. **Desktop UI** — tabs, AWS default, theming, virtualized lists, native full screen, and 50/50 comparison.
6. **Quality** — build/test gates, platform publish checks, documentation, accessibility/performance review, and smoke run.
7. **Distribution** — host-native release scripts, self-contained archives, checksums, signing/notarization options, and per-OS installation documentation.

The checked-in state file marks these baseline phases complete. A future phase can be added by creating the next numerically prefixed prompt and adding a pending entry to `automation/state.json`.
