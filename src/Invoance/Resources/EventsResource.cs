using Invoance.Exceptions;
using Invoance.Internal;
using Invoance.Models;

namespace Invoance.Resources;

/// <summary>Events resource — <c>client.Events</c>.</summary>
public sealed class EventsResource
{
    private readonly HttpTransport _t;
    internal EventsResource(HttpTransport t) => _t = t;

    /// <summary>POST /events — ingest a compliance event.</summary>
    public Task<IngestEventResponse> IngestAsync(IngestEventParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["event_type"] = p.EventType,
            ["payload"] = p.Payload,
        };
        if (!string.IsNullOrEmpty(p.EventTime)) body["event_time"] = p.EventTime;
        if (!string.IsNullOrEmpty(p.TraceId)) body["trace_id"] = p.TraceId;

        return _t.PostAsync<IngestEventResponse>("/events", body, p.IdempotencyKey, cancellationToken);
    }

    /// <summary>GET /events — paginated event listing.</summary>
    public Task<ListEventsResponse> ListAsync(ListEventsParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new ListEventsParams();
        var query = new Dictionary<string, object?>
        {
            ["page"] = p.Page,
            ["limit"] = p.Limit,
            ["date_from"] = p.DateFrom,
            ["date_to"] = p.DateTo,
            ["event_type"] = p.EventType,
        };
        return _t.GetAsync<ListEventsResponse>("/events", query, cancellationToken);
    }

    /// <summary>GET /events/{eventId} — retrieve a single event.</summary>
    public Task<ComplianceEvent> GetAsync(string eventId, CancellationToken cancellationToken = default) =>
        _t.GetAsync<ComplianceEvent>($"/events/{eventId}", null, cancellationToken);

    /// <summary>
    /// POST /events/{eventId}/verify — hash verification. Provide EITHER
    /// <see cref="VerifyEventParams.PayloadHash"/> OR
    /// <see cref="VerifyEventParams.Payload"/>. Passing neither throws
    /// <see cref="ValidationException"/>.
    /// </summary>
    public Task<VerifyEventResponse> VerifyAsync(string eventId, VerifyEventParams p, CancellationToken cancellationToken = default)
    {
        if (p.PayloadHash == null && p.Payload == null)
        {
            throw new ValidationException("events.verify requires either `PayloadHash` or `Payload`");
        }
        var body = new Dictionary<string, object?>();
        if (p.PayloadHash != null)
        {
            Validate.AssertSha256Hex("payloadHash", p.PayloadHash);
            body["payload_hash"] = p.PayloadHash;
        }
        if (p.Payload != null) body["payload"] = p.Payload;

        return _t.PostAsync<VerifyEventResponse>($"/events/{eventId}/verify", body, null, cancellationToken);
    }
}
