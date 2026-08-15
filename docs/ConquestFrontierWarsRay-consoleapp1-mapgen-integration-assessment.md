# ConsoleApp1 and MapGen Integration Assessment

This document captures how the newly added `ConsoleApp1` and `MapGen` donor projects
should be integrated into `ConquestFrontierWarsRay`.

It is intentionally scoped as a rewrite-planning note, not an implementation guide.

## Summary

The two projects should not be harvested in the same way, but the long-term
content direction is now explicit:

- XML is the intended canonical content source for imported game data
- UTF/DOS is transitional migration infrastructure, not the desired end state
- `ConsoleApp1` is the seed of the replacement XML-backed content layer
- `MapGen` is runtime-domain donor material

`ConquestFrontierWarsRay` already has the right split for this:

- `src/Data` is the current home for typed game data and repository helpers
- `src/Conquest` is still only a small executable host
- future game-facing runtime work still needs cleaner module boundaries

## ConsoleApp1

### What it is

`ConsoleApp1` is a parser harness around exported XML content.

Its current role is:

- register a large set of `BT_*`, `GT_*`, and `MT_*` XML parsers
- parse representative files from `GameTypes.db`, `GenData.db`, and `StringPack.db`
- fail fast if serializer-based loading breaks

### Integration assessment

`ConsoleApp1` should not be integrated as a console app, but it should now be
treated as donor material for the replacement content path.

Reasons:

- it already models the full exported XML shapes rather than binary field projections
- the parity coverage means the XML payload is being treated as equivalent to the DB payload
- the rewrite wants extensibility and human-readable content more than proprietary-format preservation
- importing the current console app unchanged would still duplicate responsibilities and keep the wrong project boundary

### Recommended use

Harvest it into a proper XML-first data layer:

- XML models and deserialization
- content validation
- repository APIs over exported content
- schema cross-checking and coverage auditing

Do not preserve it as a standalone runtime console harness.

Useful tooling patterns still worth retaining:

- XML fixture generation
- schema cross-checking
- parser coverage auditing
- parity-style test input discovery

Recommended destination:

- `src/Data.Xml` or equivalent if the value is canonical content loading
- `tests/` if the value is verification
- `tools/` if the value is export, auditing, or one-off acquisition

Do not wire the current console host directly into the game executable.

## MapGen

### What it is

`MapGen` is a direct C# port of the legacy `src/Conquest/MapGen.cpp` random-map generation logic.

It already owns logic for:

- choosing system counts and templates
- generating sector layouts
- selecting terrain themes
- placing planets, moons, and field objects
- generating jumpgate connections

### Integration assessment

`MapGen` is worth harvesting into the rewrite, but not as a standalone copy of the current project.

Reasons:

- it is real gameplay/runtime-domain logic rather than just data tooling
- the current rewrite already has typed `BT_MAP_GEN` support in `src/Data`
- random-map generation belongs in a dedicated local runtime module, not in a donor console app
- its long-term input should be the XML-backed typed content layer, not UTF/DOS readers

### Current mismatch with the rewrite

The current `MapGen` project is not yet a drop-in module for `ConquestFrontierWarsRay`.

Main mismatches:

- it uses a custom JSON-facing `BT_MAP_GEN` object model instead of the rewrite's canonical typed content model
- it still behaves like a console demo, with logging and debug printing as output boundaries
- some terrain/pathing surfaces are still stubbed with `NotImplementedException`
- object placement is not yet emitted as a rewrite-native mission or map result

### Recommended target shape

Harvest `MapGen` into a new local module, for example:

- `src/Runtime.MapGen`

That module should become:

- deterministic
- library-first
- independent from console I/O
- wired to the rewrite's canonical typed content layer

The console host, if still useful, should become a separate small tool project.

## Proposed integration sequence

1. Keep UTF/DOS readers alive only as migration and parity infrastructure.
2. Promote exported XML to the intended canonical content source.
3. Refactor `ConsoleApp1` into a real XML-backed data layer inside `ConquestFrontierWarsRay`.
4. Move runtime-facing repositories onto XML-backed loading once type coverage is workable.
5. Create a local map-generation runtime module inside `ConquestFrontierWarsRay`.
6. Replace `MapGen`'s custom `BT_MAP_GEN` input model with the canonical typed content model.
7. Replace console-print object placement with a structured generated-map result.
8. Add deterministic seed-based tests for map generation.
9. Retire UTF/DOS from the main content path once XML-backed loading is complete enough.

## Recommended boundaries

### Keep in `Data`

- typed DB/XML models during transition
- typed DB models
- repository access
- parity and content-verification tests

Long-term note:

- the permanent value here is the typed model and repository surface, not the UTF/DOS implementation

### Keep out of `Data`

- random-map generation algorithms
- gameplay setup DTOs that belong to runtime flow
- map instantiation logic

### Put in a new runtime module

- map generation orchestration
- sector/system layout logic
- terrain and object placement logic
- jumpgate generation
- generated-map result models

## Risks

Main risks if harvested without reshaping:

- duplicated parser stacks
- competing `BT_MAP_GEN` contracts
- runtime code coupled to donor-console assumptions
- incomplete terrain helpers pulled into production paths

Main migration risks specific to the XML-first plan:

- incomplete XML coverage for types or files not yet exercised
- deserialization drift when exported names change without tests
- leaving binary readers embedded in runtime-facing repositories for too long

## Bottom line

- XML-backed typed content is the intended replacement architecture
- UTF/DOS should be treated as transitional compatibility infrastructure
- `ConsoleApp1` should be harvested into that replacement content layer
- `MapGen` should be harvested as a dedicated runtime module after adapting it to the canonical typed content contracts

That split matches the rewrite goal of removing proprietary-format liability while
keeping a staged migration path.
