# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `InvoanceClient.MeAsync()` — `GET /v1/me` key introspection, returning the
  decoded response (`organization`, `tenant`, `api_key`, `limits`) as an
  untyped `JsonNode`.
- **Audit org lifecycle** — `client.Audit.Orgs` gains
  `UpdateAsync(organizationId, new UpdateAuditOrgParams { Name = ... })`
  (PATCH rename; a null `Name` is sent as JSON null and clears it),
  `ArchiveAsync(organizationId)` and `UnarchiveAsync(organizationId)`
  (idempotent; archiving freezes new activity while history stays verifiable),
  and `DeleteAsync(organizationId)` (hard delete, only when nothing signed
  would be destroyed — the API returns 409 `org_not_deletable` otherwise).
- `Audit.Orgs.ListAsync(new ListAuditOrgsParams { IncludeArchived = true })` —
  archived orgs are excluded by default; set `IncludeArchived` to include them
  (maps to `?include_archived=true`). Org objects now carry `archived_at`
  (string or null).
- `HttpTransport` gains the PATCH verb (`PatchAsync<T>` / `PatchRawAsync`,
  mirroring PUT).

### Changed

- `ValidateAsync()` now probes `GET /v1/me` (scope-free key introspection)
  instead of `GET /v1/events?limit=1`. Keys holding only `audit:*` scopes now
  validate correctly — the old events probe could 403 on a missing read scope
  and misreport. The `ValidationResult` shape and classification are
  unchanged; the only semantic adjustment is the 403 reason text, which now
  reads "API key authenticated but request blocked by IP access rules" (the
  previous "lacks permission to list events" wording encoded the events-probe
  quirk). 403 still reports `Valid = true`.
- `HttpTransport.GetRawAsync` gained an optional query-parameters argument and
  `Audit.Orgs.ListAsync` an optional `ListAuditOrgsParams` argument. The
  v0.1.0 positional signatures (`GetRawAsync(path, ct)` /
  `Orgs.ListAsync(ct)`) are preserved as explicit overloads, so existing
  callers keep compiling and binding. Boolean query values are serialized
  lowercase (`true`/`false`).

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
