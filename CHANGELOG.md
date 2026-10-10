# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

## [Unreleased]

### Changed
- **The packages from this repository depend on each other at exactly the same version** (`[x.y.z]`), not a floor.
  A consumer that moves one of them while another resolves at an older version now gets restore warning NU1608 naming
  the pair (an error where warnings are errors) — before, the mixed versions restored silently and could fail at run time.

## [0.14.0] - 2026-10-07

### Added
- **Bank account numbers, passport numbers, URL credentials and MAC addresses are masked.** Built-in detectors now
  report `BankAccount` (an IBAN by its check digits; a 10-16 digit number such as `110-234-567890` after an account word
  like `account` or `계좌` on the same line), `Passport` (`M12345678`, `M123A4567` or nine digits after a passport word)
  and `UrlCredential` (the value of a credential-named URL parameter such as `token` or `api_key`, and the password of
  `scheme://user:password@host` - only the secret is masked, the URL stays readable), and the new
  `PIIType.HardwareAddress` (`00:1A:2B:3C:4D:5E`, `00-1A-…`, `001a.2b3c.4d5e`; token `[MAC]`). Before, `TypesToMask = All`
  left all four in the text.
- **`PIIMasker.CoveredTypes`: the types a masker can actually find** with its options and registered detectors. `All`
  also names categories only a registered detector reports (`PersonName`, `Address`, `DriversLicense`, `TaxId`,
  `SocialSecurityNumber`); their documentation now says so.

- **Values of machine-identifier keys are not masked.** `PIIMaskingOptions.NonPiiKeys` (default
  `DefaultNonPiiKeys`: audit ids `uid`/`auid`/`ses`, `pid`/`tid`, ports, sizes, sequence numbers, timestamps, journald
  fields, ...) names keys whose `key=value`, `key: value` or `"key": value` is never PII, whatever its shape; a segment of a
  compound identifier such as a journald cursor (`...;m=0161431588;t=...`, `;`-separated `k=v` segments, two or more of the others hex ids of 8+ characters)
  is not reported either. Before, `uid=0161431588` and the cursor's `m=` field were masked as phone numbers, breaking
  the identifier. The same number under another key is still masked; add keys or clear the set to change it.

### Changed
- **Breaking**: `PIIType.URL` is `PIIType.UrlCredential` (same flag value), token `[CREDENTIAL]` instead of `[URL]` - the
  type masks a URL's secret, not the URL. Migration: rename references; a custom token keyed on `URL` keys on
  `UrlCredential`.

### Fixed
- **A ten-character hostname is no longer masked as a German ID card number.** The Personalausweis pattern matched any
  letter followed by nine letters or digits, case-insensitively, and never computed the check digit it described
  (`Pitsdshdb1` -> `[NATIONAL_ID]`). It now takes upper case and the card's alphabet only (digits and consonants without
  vowels) and checks the 7-3-1 check digit (valid 0.95, wrong check digit 0.85).
- **The password@host of a URL is no longer read as an email address**, so `postgres://app:secret@db.internal` masks the
  password instead of leaving it behind a low-confidence email match.

## [0.13.0] - 2026-10-07

### Changed
- **A table's caption names its chunks even when notes sit between them.** A caption-shaped line within 8 non-blank
  lines above a table (`<표 N> …`, `[표 N] …`, `표 N. …` / `표 N-1 …`, `Table N: …`, `Tab. N …`; figure captions are not
  used) takes the first `table_context` slot; the short lines directly above the table fill the rest. Footnote and note
  lines (`3. … 한다.`, `4) …`, `*`, `※`, `주)`, `Note:`, `자료출처:`, `출처:`, `Source:`) are skipped instead of ending the
  look-back. The search stops at a table row, so a caption of the table above is not taken. Without a caption the
  behaviour is unchanged. `table_context` and table chunk text change for such documents — re-chunk to pick it up.

## [0.12.0] - 2026-10-06

### Added
- **A table chunk carries the line(s) that name the table.** The short lines directly above a table (a title, a
  `(단위: …)` / unit line — at most 80 characters, not ending like a sentence), or else the nearest heading, are
  repeated at the top of each of the table's chunks above the header row, so a numbers-only table piece is found by the
  words that name the table. The lines stay in the preceding text chunk as well; the repeated text is in the chunk's
  `table_context` metadata. `ChunkOptions.TableContextLines` (default 2; 0 turns it off). Table chunk text changes for
  documents with such lines — re-chunk to pick it up.

## [0.11.0] - 2026-10-06

### Changed
- **Tables stay whole in every chunking strategy.** A Markdown pipe table becomes its own chunk when it fits
  `MaxChunkSize`; a larger one is split only between rows, with its header and delimiter rows repeated at the top of
  every piece, so a chunk holding table rows always names its columns and no chunk or overlap starts inside a row.
  Table chunks carry `Metadata.Custom` `table`, `table_index` (which table of the input), `table_piece`/`table_pieces`
  and `table_row_start`/`table_row_end`.
  Applies to the built-in strategies from `ChunkerFactory` (a chunker you register yourself is returned as is);
  `ChunkOptions.PreserveTables = false` restores the previous behaviour. `TableAwareChunker` wraps any `IChunker`.

## [0.10.4] - 2026-10-02

### Fixed
- **Packages now carry the license text.** Each `.nupkg` includes `LICENSE` next to the `MIT` expression,
  so an application that ships third-party notices can copy the copyright line from the package.

## [0.10.3] - 2026-10-01

### Changed
- Re-pinned sibling package(s) `Flux.Abstractions` 0.26.0 -> 0.27.0. No source changes.

## [0.10.2] - 2026-09-30

### Fixed
- **An IPv6 address after a label that ends in a hex letter is detected.** `id:2001:db8::1` and `addr:fe80::1` were
  missed because the letter before the colon was read as the end of a longer address. An address inside a longer hex
  run (`deadbeef:2001:db8::1`) is still not reported.
- The email detector no longer lists Korean second-level domains it could never compare; `co.kr`-style domains keep
  the same confidence through their `kr` suffix.

### Changed
- **Documentation describes behaviour only.** The README's architecture diagram shows a generic application layer.

## [0.10.1] - 2026-09-30

### Fixed
- **PII detectors no longer mask part of a longer number or identifier.** Patterns had no boundaries, so a slice of
  a longer run was reported: a 13-digit timestamp, order number or amount as a 12-digit national ID, a hex trace ID
  or hash as a German ID, a UUID as a phone number or (its last group) a 12-digit ID. Every built-in detector now
  matches whole values only - not inside a run of ASCII letters or digits, and a numeric ID not as one group of a
  longer hyphenated number (IPv4 and IPv6 likewise not inside a longer dotted or colon-separated run). A value glued
  to text in a script without spaces, such as `연락처010-1234-5678로`, is still detected; IPv4 addresses in that
  position were previously missed and now match too.
- **Bare runs of digits are no longer masked as phone numbers.** The US format accepted `9876543210`, and the Korean
  service-number format `15000000` or `16777216`, with no separators, so counts, sizes and amounts were masked. US
  numbers now need separators or a parenthesised area code (`234-567-8900`, `(234) 567-8900`) and service numbers a
  separator (`1588-1234`); unseparated Korean mobile and landline numbers (`01012345678`) still match.
- **Phone numbers with a parenthesised area code are detected.** `(02) 555-1234`, `(031) 123-4567` and
  `(051)1234-5678` were not matched at all.
- **Email addresses with non-ASCII characters are detected.** Local parts and domains in any script (`홍길동@회사.kr`,
  `user@회사.한국`, `müller@example.de`) were not matched. A particle written directly after an ASCII top-level
  domain (`홍길동@회사.kr로`) is not taken into the address.
- **`ContainsPII` on a detector no longer stops at the first candidate.** When the first pattern match failed
  validation, `PIIDetectorBase.ContainsPII` returned `false` even if a later match in the same text was valid.

- **`PIIMasker.ContainsPII` answers what `Mask` would mask.** It asked each detector directly, so it ignored
  `MinConfidence` and overlap resolution and could report PII in a text that `Mask` returned unchanged.
- **The README no longer lists `BankAccount` and `URL` as detected.** No built-in detector reports either type; the
  enum values remain for custom detectors.

Custom detectors deriving from `PIIDetectorBase` are unaffected; they can opt into the same matching with the new
protected `TokenStart`/`TokenEnd` and `NumberStart`/`NumberEnd` constants.

## [0.10.0] - 2026-09-30

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
- Re-pinned sibling package(s) `Flux.Abstractions` 0.25.0 -> 0.26.0 — re-consumption of already-consumed iyulab packages. No source changes.

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
