# Invoance .NET SDK

Official .NET SDK for the [Invoance](https://invoance.com) compliance API — cryptographic proof, document anchoring, and AI attestation.

## Install

```bash
dotnet add package Invoance
```

Targets **.NET 8.0** (LTS).

## Quick start

Set your API key:

```bash
export INVOANCE_API_KEY=invoance_live_...
```

```csharp
using System.Security.Cryptography;
using System.Text;
using Invoance;
using Invoance.Models;

// Reads INVOANCE_API_KEY and INVOANCE_BASE_URL from the environment.
using var client = new InvoanceClient();

// Ingest a compliance event
var ev = await client.Events.IngestAsync(new IngestEventParams
{
    EventType = "policy.approval",
    Payload = new Dictionary<string, object?> { ["policy_id"] = "pol_001", ["decision"] = "approved" },
});
Console.WriteLine(ev.EventId);

// Anchor a document by hash
var docBytes = Encoding.UTF8.GetBytes("...your document bytes...");
var doc = await client.Documents.AnchorAsync(new AnchorDocumentParams
{
    DocumentHash = Convert.ToHexString(SHA256.HashData(docBytes)).ToLowerInvariant(),
    DocumentRef = "Invoice #1042",
});
Console.WriteLine(doc.EventId);

// Or use the file helper (hashes + uploads in one call)
var anchored = await client.Documents.AnchorFileAsync(new AnchorFileParams
{
    Path = "./invoice.pdf",
    DocumentRef = "Invoice #1042",
});

// Ingest an AI attestation
var att = await client.Attestations.IngestAsync(new IngestAttestationParams
{
    Type = "output",
    Input = "Summarize this contract",
    Output = "The contract states...",
    ModelProvider = "openai",
    ModelName = "gpt-4o",
    ModelVersion = "2025-01-01",
    Subject = new AttestationSubject { UserId = "u_42", SessionId = "sess_4f9a" },
});
Console.WriteLine(att.AttestationId);
```

## Quick validation

Sanity-check that your API key works before wiring the SDK into a larger app:

```csharp
using var client = new InvoanceClient();
var (valid, reason, baseUrl) = await client.ValidateAsync();
if (!valid) throw new Exception($"Invoance: {reason} (base: {baseUrl})");
```

`ValidateAsync()` probes `GET /v1/events?limit=1`, never throws, and returns `{ Valid, Reason, BaseUrl }` — use it in health checks, startup scripts, or CI guards.

## Configuration

The client reads from environment variables automatically:

| Variable | Required | Default |
|---|---|---|
| `INVOANCE_API_KEY` | Yes | — |
| `INVOANCE_BASE_URL` | No | `https://api.invoance.com` |

You can also pass options explicitly:

```csharp
using var client = new InvoanceClient(new InvoanceClientOptions
{
    ApiKey = "invoance_live_...",
    Timeout = TimeSpan.FromSeconds(60),
});
```

Every network method is asynchronous, named `...Async`, returns a `Task<T>`, and accepts a
trailing `CancellationToken`.

## Error handling

Every error the SDK raises — API responses, network failures, client-side validation —
inherits from `InvoanceException`:

```csharp
using Invoance.Exceptions;

try
{
    await client.Events.IngestAsync(new IngestEventParams
    {
        EventType = "user.login",
        Payload = new Dictionary<string, object?>(),
    });
}
catch (AuthenticationException)
{
    // 401 — bad API key
}
catch (QuotaExceededException e)
{
    Console.WriteLine($"rate limited, retry in {e.RetryAfterSeconds}s");
}
catch (ValidationException e)
{
    // 400 or bad client-side input
}
catch (InvoanceException e)
{
    Console.WriteLine($"Invoance error: {e.Message}");
}
```

| Exception | Trigger |
|---|---|
| `AuthenticationException` | HTTP 401 |
| `ForbiddenException` | HTTP 403 |
| `NotFoundException` | HTTP 404 |
| `ValidationException` | HTTP 400 / bad client input |
| `ConflictException` | HTTP 409 |
| `QuotaExceededException` | HTTP 429 |
| `ServerException` | HTTP 5xx |
| `NetworkException` | transport failure before a response |
| `TimeoutException` | exceeded configured timeout |

## Offline verification

The SDK can verify signatures client-side, without trusting the server:

```csharp
// Ed25519 signature of an AI attestation
var result = await client.Attestations.VerifySignatureAsync(att.AttestationId);
Console.WriteLine(result.Valid);

// Verify by the raw canonical payload string (order-preserving)
await client.Attestations.VerifyPayloadAsync(att.AttestationId, rawJsonString);

// Offline audit-event verification
using Invoance.Internal;
var verify = AuditVerify.VerifyAuditEvent(auditEventMap);       // key from the event
var pinned = AuditVerify.VerifyAuditEvent(auditEventMap, tenantPubKeyHex); // pinned key
```

## Resources

Every resource hangs off the client as a property (`client.Events`, `client.Documents`, …).
All network methods are asynchronous, end in `Async`, return a `Task<T>`, and accept an
optional trailing `CancellationToken`.

### Events

```csharp
// POST /events — ingest a compliance event.
var ev = await client.Events.IngestAsync(new IngestEventParams
{
    EventType = "user.login",
    Payload = new Dictionary<string, object?> { ["user_id"] = "u_42" },
    EventTime = "2026-07-06T12:00:00Z", // optional
    TraceId = "trc_...",                // optional — group into a trace
    IdempotencyKey = "idem_...",        // optional — safe retries
});
Console.WriteLine($"{ev.EventId} @ {ev.IngestedAt}");

// GET /events — paginated listing.
var page = await client.Events.ListAsync(new ListEventsParams
{
    Page = 1,
    Limit = 50,
    DateFrom = "2026-07-01",
    DateTo = "2026-07-31",
    EventType = "user.login",
});
Console.WriteLine($"{page.Total} events, has_more={page.HasMore}");
foreach (var item in page.Events) Console.WriteLine(item.EventId);

// GET /events/{eventId} — retrieve a single event.
ComplianceEvent full = await client.Events.GetAsync(ev.EventId);
Console.WriteLine(full.EventHash);

// POST /events/{eventId}/verify — hash verification.
// Provide EXACTLY ONE of PayloadHash or Payload (passing neither throws ValidationException).
var byPayload = await client.Events.VerifyAsync(ev.EventId, new VerifyEventParams
{
    Payload = new Dictionary<string, object?> { ["user_id"] = "u_42" },
});
var byHash = await client.Events.VerifyAsync(ev.EventId, new VerifyEventParams
{
    PayloadHash = "e3b0c442...", // 64-char hex SHA-256
});
Console.WriteLine(byPayload.MatchResult);
```

### Documents

```csharp
using System.Security.Cryptography;
using System.Text;

// POST /document/anchor — anchor a document by its SHA-256 hash.
var docBytes = Encoding.UTF8.GetBytes("...your document bytes...");
var doc = await client.Documents.AnchorAsync(new AnchorDocumentParams
{
    DocumentHash = Convert.ToHexString(SHA256.HashData(docBytes)).ToLowerInvariant(),
    DocumentRef = "Invoice #1042",
    EventType = "invoice.issued",                                       // optional
    OriginalBytesB64 = Convert.ToBase64String(docBytes),               // optional — store original
    Metadata = new Dictionary<string, object?> { ["amount"] = 4200 },  // optional
    TraceId = "trc_...",                                               // optional
    IdempotencyKey = "idem_...",                                       // optional
});
Console.WriteLine($"{doc.EventId} {doc.Status}");

// Convenience: hash + base64-encode + anchor in one call (from a path or raw bytes).
var fromPath = await client.Documents.AnchorFileAsync(new AnchorFileParams
{
    Path = "./invoice.pdf",
    DocumentRef = "Invoice #1042", // defaults to the filename when omitted
});
var fromBytes = await client.Documents.AnchorFileAsync(new AnchorFileParams
{
    Bytes = docBytes,
    DocumentRef = "blob",
    SkipOriginal = true, // don't upload the original bytes
});

// GET /document — paginated listing.
var docs = await client.Documents.ListAsync(new ListDocumentsParams
{
    Page = 1,
    Limit = 50,
    DateFrom = "2026-07-01",
    DateTo = "2026-07-31",
    DocumentRef = "Invoice #1042",
});
Console.WriteLine($"{docs.Total} documents");

// GET /document/{eventId} — retrieve a single document record.
DocumentEvent record = await client.Documents.GetAsync(doc.EventId);
Console.WriteLine($"{record.DocumentHash} has_original={record.HasOriginal}");

// GET /document/{eventId}/original — download the original file bytes.
byte[] original = await client.Documents.GetOriginalAsync(doc.EventId);
await File.WriteAllBytesAsync("./restored.pdf", original);

// POST /document/{eventId}/verify — hash verification.
var result = await client.Documents.VerifyAsync(doc.EventId, new VerifyDocumentParams
{
    DocumentHash = Convert.ToHexString(SHA256.HashData(docBytes)).ToLowerInvariant(),
});
Console.WriteLine(result.MatchResult);
```

### AI Attestations

```csharp
// POST /ai/attestations — anchor an AI attestation.
var att = await client.Attestations.IngestAsync(new IngestAttestationParams
{
    Type = "output",
    Input = "Summarize this contract",
    Output = "The contract states...",
    ModelProvider = "openai",
    ModelName = "gpt-4o",
    ModelVersion = "2025-01-01",
    Subject = new AttestationSubject   // optional
    {
        UserId = "u_42",
        SessionId = "sess_4f9a",
        Extra = new Dictionary<string, object?> { ["tenant"] = "acme" }, // custom context
    },
    TraceId = "trc_...",         // optional
    IdempotencyKey = "idem_...", // optional
});
Console.WriteLine($"{att.AttestationId} payload_hash={att.PayloadHash}");

// GET /ai/attestations — paginated listing.
var atts = await client.Attestations.ListAsync(new ListAttestationsParams
{
    Page = 1,
    Limit = 50,
    DateFrom = "2026-07-01",
    DateTo = "2026-07-31",
    AttestationType = "output",
    ModelProvider = "openai",
});
Console.WriteLine($"{atts.Total} attestations");

// GET /ai/attestations/{id} — retrieve a single attestation.
AiAttestation one = await client.Attestations.GetAsync(att.AttestationId);
Console.WriteLine($"{one.SignatureAlg} {one.PublicKey}");

// POST /ai/attestations/{id}/verify — server-side hash verification.
var verify = await client.Attestations.VerifyAsync(att.AttestationId, new VerifyAttestationParams
{
    ContentHash = "a1b2c3...", // 64-char hex SHA-256
});
Console.WriteLine(verify.MatchResult);

// GET /ai/attestations/{id}/raw — the original canonical JSON payload as a node.
System.Text.Json.Nodes.JsonNode? raw = await client.Attestations.GetRawAsync(att.AttestationId);

// Verify by raw payload — hashes client-side, then calls verify.
// Overloads: string / byte[] / JsonNode / IDictionary<string, object?>.
await client.Attestations.VerifyPayloadAsync(att.AttestationId, rawJsonString);

// Fully client-side Ed25519 signature verification (no trust in the server).
SignatureVerificationResult sig = await client.Attestations.VerifySignatureAsync(att.AttestationId);
Console.WriteLine($"valid={sig.Valid} reason={sig.Reason}");
```

> **Note:** for `VerifyPayloadAsync`, pass the raw JSON string exactly as shown in the
> dashboard's "Raw immutable record" viewer. Key order is **preserved** (not sorted) because
> the backend hashes with struct field order `type/payload/context/subject`.

### Traces

```csharp
// POST /traces — create a new trace.
var trace = await client.Traces.CreateAsync(new CreateTraceParams
{
    Label = "Batch 2026-07",
    Metadata = new Dictionary<string, object?> { ["run"] = 42 }, // optional
});
Console.WriteLine($"{trace.TraceId} {trace.Status}");

// GET /traces — paginated listing (filter by status: "open" or "sealed").
var traces = await client.Traces.ListAsync(new ListTracesParams { Status = "open", Limit = 50 });
Console.WriteLine($"{traces.Total} traces");

// GET /traces/{traceId} — trace detail with paginated events.
TraceDetail detail = await client.Traces.GetAsync(trace.TraceId, new GetTraceParams
{
    EventPage = 1,
    EventLimit = 50,
});
Console.WriteLine($"{detail.EventCount} events, composite_hash={detail.CompositeHash}");

// POST /traces/{traceId}/seal — seal a trace (async, returns 202).
SealTraceResponse seal = await client.Traces.SealAsync(trace.TraceId);
Console.WriteLine($"{seal.Status}: {seal.Message}");

// GET /traces/{traceId}/proof — export the proof bundle as JSON.
TraceProofBundle proof = await client.Traces.ProofAsync(trace.TraceId);
Console.WriteLine(proof.Verification?.CompositeHashValid);

// GET /traces/{traceId}/proof/pdf — download the proof bundle as PDF bytes.
byte[] pdf = await client.Traces.ProofPdfAsync(trace.TraceId);
await File.WriteAllBytesAsync("./trace-proof.pdf", pdf);

// DELETE /traces/{traceId} — delete an empty, open trace.
DeleteTraceResponse del = await client.Traces.DeleteAsync(trace.TraceId);
Console.WriteLine(del.Deleted);
```

### Audit logs

An append-only, per-tenant signed event ledger with end-customer orgs, webhook streams,
hosted-viewer portal links, and async exports. Methods under `client.Audit` are grouped into
sub-resources: `.Events`, `.Orgs`, `.Streams`, `.PortalSessions`, and `.Exports`.

```csharp
// ── client.Audit.Orgs ────────────────────────────────────────
await client.Audit.Orgs.CreateAsync(new CreateAuditOrgParams
{
    OrganizationId = "org_1",
    Name = "Acme", // optional
});
await client.Audit.Orgs.ListAsync();
await client.Audit.Orgs.IntegrityAsync("org_1");        // hash-chain integrity check
await client.Audit.Orgs.SetRetentionAsync("org_1", 365); // retention in days

// ── client.Audit.Events ──────────────────────────────────────
// POST /audit/events — append one signed event (Idempotency-Key is required;
// the SDK derives a content-stable one when IdempotencyKey is omitted).
System.Text.Json.Nodes.JsonNode? appended = await client.Audit.Events.IngestAsync(new IngestAuditEventParams
{
    OrganizationId = "org_1",
    Action = "invoice.approved",
    Actor = new Dictionary<string, object?> { ["type"] = "user", ["id"] = "u_1" },
    Targets = new List<IDictionary<string, object?>>
    {
        new Dictionary<string, object?> { ["type"] = "invoice", ["id"] = "inv_9" },
    },
    OccurredAt = "2026-07-06T12:00:00Z",                                 // optional
    Context = new Dictionary<string, object?> { ["ip"] = "1.2.3.4" },    // optional
    Metadata = new Dictionary<string, object?> { ["note"] = "manual" },  // optional
});

// GET /audit/events — keyset-paginated listing.
ListAuditEventsResponse events = await client.Audit.Events.ListAsync(new ListAuditEventsParams
{
    OrganizationId = "org_1",
    Actions = "invoice.approved",
    ActorId = "u_1",
    TargetId = "inv_9",
    RangeStart = "2026-07-01T00:00:00Z",
    RangeEnd = "2026-07-31T23:59:59Z",
    Limit = 100,
    Cursor = null, // pass events.NextCursor to page forward
});
Console.WriteLine($"next_cursor={events.NextCursor}");

// GET /audit/events/{id}.
AuditEvent evt = await client.Audit.Events.GetAsync("evt_123");
Console.WriteLine($"{evt.Seq} {evt.Action} {evt.PayloadHash}");

// GET /audit/events/{id}/verify — server-side verify against the pinned key.
await client.Audit.Events.VerifyAsync("evt_123");

// ── client.Audit.Streams ─────────────────────────────────────
// The signing secret is returned ONCE on create.
await client.Audit.Streams.CreateAsync("org_1", new CreateAuditStreamParams
{
    Url = "https://siem.example/hook",
    Type = "webhook", // optional — v1 supports "webhook" only
});
await client.Audit.Streams.ListAsync("org_1");
await client.Audit.Streams.TestAsync("org_1", "stream_1"); // send a test delivery
await client.Audit.Streams.DeleteAsync("org_1", "stream_1");

// ── client.Audit.PortalSessions ──────────────────────────────
// Mint a one-time hosted-viewer link.
await client.Audit.PortalSessions.CreateAsync(new CreatePortalSessionParams
{
    OrganizationId = "org_1",
    Intent = "audit_logs",           // or "log_streams"
    SessionDurationSeconds = 3600,   // optional
    LinkDurationSeconds = 600,       // optional
});

// ── client.Audit.Exports ─────────────────────────────────────
// Queue an async export job, then poll until it's ready.
System.Text.Json.Nodes.JsonNode? job = await client.Audit.Exports.CreateAsync(new CreateAuditExportParams
{
    OrganizationId = "org_1",
    Format = "csv", // or "ndjson"
    Filters = new Dictionary<string, object?> { ["action"] = "invoice.approved" }, // optional
});
var status = await client.Audit.Exports.GetAsync("exp_123"); // download_url appears when status == "ready"
```

Offline-verify an audit event returned by the API:

```csharp
using Invoance.Internal;

// Fetch the event as an untyped wire map, then verify its signature client-side.
var evt = await client.Audit.Events.GetAsync("evt_123");
var map = new Dictionary<string, object?>
{
    ["id"] = evt.Id,
    ["org_id"] = evt.OrgId,
    ["seq"] = evt.Seq,
    ["occurred_at"] = evt.OccurredAt,
    ["ingested_at"] = evt.IngestedAt,
    ["action"] = evt.Action,
    ["actor"] = evt.Actor,
    ["targets"] = evt.Targets,
    ["signature"] = evt.Signature,
    ["signing_public_key"] = evt.SigningPublicKey,
};

AuditVerifyResult result = AuditVerify.VerifyAuditEvent(map);
Console.WriteLine($"valid={result.Valid} key_source={result.KeySource}");

// Pin against the tenant's registered key for a real tamper guarantee:
AuditVerify.VerifyAuditEvent(map, registeredHexKey);
```

## License

MIT — see [LICENSE](./LICENSE).
