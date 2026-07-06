// Invoance .NET SDK — runnable examples.
//
//   export INVOANCE_API_KEY=invoance_live_...
//   dotnet run --project examples/Quickstart -- <example>
//
// where <example> is one of: quickstart | events | documents | attestations
//                            | traces | audit | validate  (default: quickstart)

using System.Security.Cryptography;
using System.Text;
using Invoance;
using Invoance.Exceptions;
using Invoance.Models;

var which = args.Length > 0 ? args[0].ToLowerInvariant() : "quickstart";

// Reads INVOANCE_API_KEY / INVOANCE_BASE_URL from the environment.
using var client = new InvoanceClient();

try
{
    switch (which)
    {
        case "validate":
            await ValidateExample(client);
            break;
        case "events":
            await EventsExample(client);
            break;
        case "documents":
            await DocumentsExample(client);
            break;
        case "attestations":
            await AttestationsExample(client);
            break;
        case "traces":
            await TracesExample(client);
            break;
        case "audit":
            await AuditExample(client);
            break;
        default:
            await QuickstartExample(client);
            break;
    }
}
catch (AuthenticationException)
{
    Console.Error.WriteLine("Authentication failed — check INVOANCE_API_KEY.");
}
catch (QuotaExceededException e)
{
    Console.Error.WriteLine($"Rate limited; retry after {e.RetryAfterSeconds}s.");
}
catch (InvoanceException e)
{
    Console.Error.WriteLine($"Invoance error: {e.Message}");
}

static async Task ValidateExample(InvoanceClient client)
{
    var (valid, reason, baseUrl) = await client.ValidateAsync();
    Console.WriteLine($"valid={valid} reason={reason} baseUrl={baseUrl}");
}

static async Task QuickstartExample(InvoanceClient client)
{
    var ev = await client.Events.IngestAsync(new IngestEventParams
    {
        EventType = "policy.approval",
        Payload = new Dictionary<string, object?> { ["policy_id"] = "pol_001", ["decision"] = "approved" },
    });
    Console.WriteLine($"event: {ev.EventId}");

    var docBytes = Encoding.UTF8.GetBytes("...your document bytes...");
    var doc = await client.Documents.AnchorAsync(new AnchorDocumentParams
    {
        DocumentHash = Convert.ToHexString(SHA256.HashData(docBytes)).ToLowerInvariant(),
        DocumentRef = "Invoice #1042",
    });
    Console.WriteLine($"document: {doc.EventId}");

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
    Console.WriteLine($"attestation: {att.AttestationId}");
}

static async Task EventsExample(InvoanceClient client)
{
    var ev = await client.Events.IngestAsync(new IngestEventParams
    {
        EventType = "user.login",
        Payload = new Dictionary<string, object?> { ["user_id"] = "u_42" },
    });
    Console.WriteLine($"ingested {ev.EventId}");

    var list = await client.Events.ListAsync(new ListEventsParams { Limit = 5 });
    Console.WriteLine($"total events: {list.Total}");

    var verify = await client.Events.VerifyAsync(ev.EventId, new VerifyEventParams
    {
        Payload = new Dictionary<string, object?> { ["user_id"] = "u_42" },
    });
    Console.WriteLine($"match: {verify.MatchResult}");
}

static async Task DocumentsExample(InvoanceClient client)
{
    // Hash + upload a file in one call.
    var tmp = Path.GetTempFileName();
    await File.WriteAllTextAsync(tmp, "hello invoance");
    var anchored = await client.Documents.AnchorFileAsync(new AnchorFileParams
    {
        Path = tmp,
        DocumentRef = "greeting.txt",
    });
    Console.WriteLine($"anchored {anchored.EventId} hash={anchored.DocumentHash}");
}

static async Task AttestationsExample(InvoanceClient client)
{
    var att = await client.Attestations.IngestAsync(new IngestAttestationParams
    {
        Type = "output",
        Input = "prompt",
        Output = "completion",
        ModelProvider = "anthropic",
        ModelName = "claude",
        ModelVersion = "1",
    });

    var sig = await client.Attestations.VerifySignatureAsync(att.AttestationId);
    Console.WriteLine($"signature valid: {sig.Valid}");
}

static async Task TracesExample(InvoanceClient client)
{
    var trace = await client.Traces.CreateAsync(new CreateTraceParams { Label = "onboarding-run-42" });
    Console.WriteLine($"trace {trace.TraceId}");
    await client.Traces.SealAsync(trace.TraceId);
    Console.WriteLine("seal requested");
}

static async Task AuditExample(InvoanceClient client)
{
    var raw = await client.Audit.Events.IngestAsync(new IngestAuditEventParams
    {
        OrganizationId = "org_customer_1",
        Action = "invoice.approved",
        Actor = new Dictionary<string, object?> { ["type"] = "user", ["id"] = "u_1" },
        Targets = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["type"] = "invoice", ["id"] = "inv_9" },
        },
    });
    Console.WriteLine($"audit event: {raw}");
}
