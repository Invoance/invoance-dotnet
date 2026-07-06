using Invoance.Internal;
using Invoance.Models;

namespace Invoance.Resources;

/// <summary>Traces resource — <c>client.Traces</c>.</summary>
public sealed class TracesResource
{
    private readonly HttpTransport _t;
    internal TracesResource(HttpTransport t) => _t = t;

    /// <summary>POST /traces — create a new trace.</summary>
    public Task<CreateTraceResponse> CreateAsync(CreateTraceParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?> { ["label"] = p.Label };
        if (p.Metadata != null) body["metadata"] = p.Metadata;
        return _t.PostAsync<CreateTraceResponse>("/traces", body, null, cancellationToken);
    }

    /// <summary>GET /traces — paginated trace listing.</summary>
    public Task<ListTracesResponse> ListAsync(ListTracesParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new ListTracesParams();
        var query = new Dictionary<string, object?>
        {
            ["page"] = p.Page,
            ["limit"] = p.Limit,
            ["status"] = p.Status,
        };
        return _t.GetAsync<ListTracesResponse>("/traces", query, cancellationToken);
    }

    /// <summary>GET /traces/{traceId} — trace detail with paginated events.</summary>
    public Task<TraceDetail> GetAsync(string traceId, GetTraceParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new GetTraceParams();
        var query = new Dictionary<string, object?>
        {
            ["event_page"] = p.EventPage,
            ["event_limit"] = p.EventLimit,
        };
        return _t.GetAsync<TraceDetail>($"/traces/{traceId}", query, cancellationToken);
    }

    /// <summary>DELETE /traces/{traceId} — delete an empty open trace.</summary>
    public Task<DeleteTraceResponse> DeleteAsync(string traceId, CancellationToken cancellationToken = default) =>
        _t.DeleteAsync<DeleteTraceResponse>($"/traces/{traceId}", cancellationToken);

    /// <summary>POST /traces/{traceId}/seal — seal a trace (async, 202).</summary>
    public Task<SealTraceResponse> SealAsync(string traceId, CancellationToken cancellationToken = default) =>
        _t.PostAsync<SealTraceResponse>($"/traces/{traceId}/seal", new Dictionary<string, object?>(), null, cancellationToken);

    /// <summary>GET /traces/{traceId}/proof — export proof bundle as JSON.</summary>
    public Task<TraceProofBundle> ProofAsync(string traceId, CancellationToken cancellationToken = default) =>
        _t.GetAsync<TraceProofBundle>($"/traces/{traceId}/proof", null, cancellationToken);

    /// <summary>GET /traces/{traceId}/proof/pdf — download proof bundle as PDF bytes.</summary>
    public Task<byte[]> ProofPdfAsync(string traceId, CancellationToken cancellationToken = default) =>
        _t.GetBytesAsync($"/traces/{traceId}/proof/pdf", cancellationToken);
}
