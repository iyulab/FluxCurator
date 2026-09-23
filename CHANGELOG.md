# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

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
