# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

## [0.10.0] - Unreleased

### Changed
- **Breaking: the entry-point class `FluxCurator` is now `Curator`.** The class shared its name with its namespace, so
  `new FluxCurator()` after `using FluxCurator;` did not compile (CS0118) and callers had to write
  `global::FluxCurator.FluxCurator`. `IFluxCurator`, `Create()` and every method are unchanged.
  Migration: replace `FluxCurator.FluxCurator` / `new FluxCurator()` with `Curator` / `new Curator()`.

### Fixed
- **A registered PII detector now masks.** `PIIMasker.RegisterDetector` and `Curator.RegisterPIIDetector` added the
  detector under its `PIIType`, and `TypesToMask` then filtered it out: a custom detector reports `PIIType.Custom`,
  which no preset includes (not even `PIIType.All`), so it never ran and nothing reported it. A registered detector
  now always runs; `TypesToMask` selects among the built-in detectors only.
- **`WithPIIMasking(...)` after `RegisterPIIDetector` keeps the detector.** Reconfiguring the options rebuilt the
  masker without the detectors registered on the curator.
- **A language tag with a region uses its language's profile.** `ChunkOptions.LanguageCode = "zh-TW"` (or `"pt-BR"`,
  `"en-US"`) matched no profile and fell back to English sentence and token rules without a word; it now resolves to
  the `zh` (`pt`, `en`) profile. Unknown languages still fall back to English.
- **The README's code compiles.** Every C# block is compiled against the current API in CI. The fixes: the entry point
  (above), the three `using` lines the examples need (stated once in Quick Start), a sample SSN the detector treats as
  a known test number, custom detector examples whose type declarations followed their statements, a custom national
  ID example for a country the library already covers, and a FileFlux example naming types from another package.
- **The guides under `docs/` match the API and are compiled in CI too.** Among the corrections: `DocumentChunk.ChunkIndex`
  (not `Index`), `PIIMaskingResult.Matches`, `FilterContent`, `PreprocessingResult`, the services `AddFluxCurator`
  actually registers (`IChunkerFactory` singleton, `IFluxCurator` transient — no separate PII or filter services), how
  `Auto` really selects a strategy (Sentence, Paragraph or Token — never Semantic or Hierarchical, and a
  `ChunkerFactory` cannot resolve it), the removed `IncludeMetadata` option, and the FileFlux integration page, which
  described a strategy type FileFlux does not have.

## [0.9.1] - 2026-09-23

### Changed
- Re-pinned sibling package(s) `Flux.Abstractions` 0.25.0 -> 0.26.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.9.0] - 2026-09-21

### Fixed
- **A split section no longer carries its header twice.** The header line was baked into a
  section's content when sections were parsed, and then prepended a second time to the first chunk
  of that section - so any section larger than `MaxChunkSize` repeated its own header. The existing
  duplication guard counted sentence markers rather than the header, so it stayed green. The header
  now enters in exactly one place.
- **`ChunkOptions.PreserveSectionHeaders` now decides whether it enters at all.** It was declared,
  documented and read by nothing: the header was carried into the first chunk unconditionally.
  **The default is unchanged** (`true` - carry it). Setting it to `false` leaves the header out of
  the chunk text while `ChunkMetadata.ContainsSectionHeader` is still stamped, so a consumer that
  suppresses the text can still tell which chunk opened a section.

### Removed
- **Breaking: six public options that nothing read.** Each was declared, documented and defaulted
  to a value that suggested it did something. Migration for every item: delete the assignment.
  - **`PIIMaskingOptions.ValidatePatterns`.** Worth reading twice if you set it: checksum
    validation is real and runs on every match - Luhn for cards, ISO 7064, Modulo-97 and a dozen
    national schemes - but detectors take no options object, so the switch had nowhere to land and
    `false` never disabled anything. Detection quality is not configurable here, and removing the
    switch is the honest statement of that.
  - **`PIIMaskingOptions.EnableParallelProcessing` and `.ParallelThreshold`.** There is no
    parallelism in the PII path: masking is a sequential loop that never inspects the text length.
    Batch-level concurrency lives in `BatchProcessor` and is configured there.
  - **`ChunkOptions.IncludeMetadata`, `ContentFilterOptions.IncludeMetadata` and
    `PIIMaskingOptions.IncludeMetadata`.** All three gated data the library always produces - and
    in the chunking case cannot stop producing, since the chunker reads
    `ChunkMetadata.EstimatedTokenCount` itself to merge and split. Neither `PIIMaskingResult` nor
    `ContentFilterResult` has a metadata field for the other two to gate.

## [0.8.3] - 2026-09-17

This file starts at 0.8.3. Changes in earlier releases were not recorded here; the commit history is
the record for them.
