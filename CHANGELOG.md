# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-06

### Added

- Initial release of the official Invoance .NET SDK.
- `ComplianceEvent` reflects the real `GET /v1/events/:id` shape: `AccessTier` is nullable (`string?`) and an `ExpiresAt` field is included.
- `InvoanceClient` with environment-based and explicit configuration
  (`InvoanceClientOptions`), plus `ValidateAsync()` health probe.
- Resources: `Events`, `Documents`, `Attestations`, `Traces`, and `Audit`
  (with `.Events`, `.Orgs`, `.Streams`, `.PortalSessions`, `.Exports`
  sub-resources). All network methods are `Task`-returning `...Async` methods
  accepting a `CancellationToken`.
- Full exception hierarchy rooted at `InvoanceException` with status→type
  mapping and `Retry-After` parsing.
- Client-side crypto: Ed25519 signature verification (via BouncyCastle), the
  `invoance.audit/1` canonicalizer, offline audit-event verification,
  attestation payload/signature verification, and content-idempotency keys.
- Byte-parity canonicalization using the JS-compatible relaxed JSON encoder.

[0.1.0]: https://github.com/Invoance/invoance-dotnet/releases/tag/v0.1.0
