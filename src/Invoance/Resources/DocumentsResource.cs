using Invoance.Internal;
using Invoance.Models;

namespace Invoance.Resources;

/// <summary>Documents resource — <c>client.Documents</c>.</summary>
public sealed class DocumentsResource
{
    private readonly HttpTransport _t;
    internal DocumentsResource(HttpTransport t) => _t = t;

    /// <summary>POST /document/anchor — anchor a document hash.</summary>
    public Task<AnchorDocumentResponse> AnchorAsync(AnchorDocumentParams p, CancellationToken cancellationToken = default)
    {
        Validate.AssertSha256Hex("documentHash", p.DocumentHash);
        var body = new Dictionary<string, object?>
        {
            ["document_hash"] = p.DocumentHash,
        };
        if (p.DocumentRef != null) body["document_ref"] = p.DocumentRef;
        if (p.EventType != null) body["event_type"] = p.EventType;
        if (p.OriginalBytesB64 != null) body["original_bytes_b64"] = p.OriginalBytesB64;
        if (p.Metadata != null) body["metadata"] = p.Metadata;
        if (p.TraceId != null) body["trace_id"] = p.TraceId;

        return _t.PostAsync<AnchorDocumentResponse>("/document/anchor", body, p.IdempotencyKey, cancellationToken);
    }

    /// <summary>
    /// Convenience helper — reads a file (path or bytes), computes the
    /// SHA-256 hash, base64-encodes the bytes, and calls
    /// <see cref="AnchorAsync"/>.
    /// </summary>
    public async Task<AnchorDocumentResponse> AnchorFileAsync(AnchorFileParams p, CancellationToken cancellationToken = default)
    {
        if (p.Path == null && p.Bytes == null)
        {
            throw new Exceptions.ValidationException("anchorFile requires either `Path` or `Bytes`");
        }

        byte[] content = p.Bytes
            ?? await File.ReadAllBytesAsync(p.Path!, cancellationToken).ConfigureAwait(false);

        var documentHash = Crypto.Sha256Hex(content);

        var documentRef = p.DocumentRef
            ?? (p.Path != null ? System.IO.Path.GetFileName(p.Path) : null);

        return await AnchorAsync(new AnchorDocumentParams
        {
            DocumentHash = documentHash,
            DocumentRef = documentRef,
            EventType = p.EventType,
            Metadata = p.Metadata,
            IdempotencyKey = p.IdempotencyKey,
            OriginalBytesB64 = p.SkipOriginal ? null : Convert.ToBase64String(content),
            TraceId = p.TraceId,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>GET /document — paginated document listing.</summary>
    public Task<ListDocumentsResponse> ListAsync(ListDocumentsParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new ListDocumentsParams();
        var query = new Dictionary<string, object?>
        {
            ["page"] = p.Page,
            ["limit"] = p.Limit,
            ["date_from"] = p.DateFrom,
            ["date_to"] = p.DateTo,
            ["document_ref"] = p.DocumentRef,
        };
        return _t.GetAsync<ListDocumentsResponse>("/document", query, cancellationToken);
    }

    /// <summary>GET /document/{eventId} — retrieve a single document.</summary>
    public Task<DocumentEvent> GetAsync(string eventId, CancellationToken cancellationToken = default) =>
        _t.GetAsync<DocumentEvent>($"/document/{eventId}", null, cancellationToken);

    /// <summary>GET /document/{eventId}/original — download the original file bytes.</summary>
    public Task<byte[]> GetOriginalAsync(string eventId, CancellationToken cancellationToken = default) =>
        _t.GetBytesAsync($"/document/{eventId}/original", cancellationToken);

    /// <summary>POST /document/{eventId}/verify — hash verification.</summary>
    public Task<VerifyDocumentResponse> VerifyAsync(string eventId, VerifyDocumentParams p, CancellationToken cancellationToken = default)
    {
        Validate.AssertSha256Hex("documentHash", p.DocumentHash);
        var body = new Dictionary<string, object?> { ["document_hash"] = p.DocumentHash };
        return _t.PostAsync<VerifyDocumentResponse>($"/document/{eventId}/verify", body, null, cancellationToken);
    }
}
