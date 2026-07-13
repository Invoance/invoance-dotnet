using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Invoance.Exceptions;
using Invoance.Models;
using Xunit;

namespace Invoance.Tests;

/// <summary>
/// Wire-shape tests for the audit org lifecycle (update / archive / unarchive /
/// delete / list) — assert the exact HTTP method, path, query, and JSON body
/// the SDK puts on the wire, against a recording message handler.
/// </summary>
public class AuditOrgsTests
{
    private const string OrgJson =
        "{\"id\":\"aorg_x\",\"organization_id\":\"org_x\",\"external_id\":\"org_x\",\"name\":\"Acme\"," +
        "\"retention_days\":365,\"created_at\":\"2026-07-01T00:00:00Z\",\"archived_at\":null}";

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        private readonly HttpStatusCode _status;

        public HttpRequestMessage? Request;
        public string? RequestBody;

        public RecordingHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responseJson = responseJson;
            _status = status;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (InvoanceClient Client, RecordingHandler Handler) MockClient(
        string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new RecordingHandler(responseJson, status);
        var client = new InvoanceClient(
            new InvoanceClientOptions { ApiKey = "sk_test_x", BaseUrl = "https://api.example.test" },
            new HttpClient(handler));
        return (client, handler);
    }

    [Fact]
    public async Task UpdateAsync_SendsPatchWithNewNameInBody()
    {
        var (client, handler) = MockClient(OrgJson.Replace("Acme", "Acme Corp"));

        var org = await client.Audit.Orgs.UpdateAsync("org_x", new UpdateAuditOrgParams { Name = "Acme Corp" });

        Assert.Equal(HttpMethod.Patch, handler.Request!.Method);
        Assert.Equal("/v1/audit/orgs/org_x", handler.Request.RequestUri!.AbsolutePath);
        var body = JsonNode.Parse(handler.RequestBody!)!;
        Assert.Equal("Acme Corp", (string?)body["name"]);
        Assert.Equal("Acme Corp", (string?)org!["name"]);
    }

    [Fact]
    public async Task UpdateAsync_SendsJsonNullNameToClearIt()
    {
        var (client, handler) = MockClient(OrgJson);

        await client.Audit.Orgs.UpdateAsync("org_x", new UpdateAuditOrgParams { Name = null });

        Assert.Equal(HttpMethod.Patch, handler.Request!.Method);
        Assert.Equal("{\"name\":null}", handler.RequestBody);
    }

    [Fact]
    public async Task ArchiveAsync_PostsToArchivePath()
    {
        var (client, handler) = MockClient(OrgJson.Replace("\"archived_at\":null", "\"archived_at\":\"2026-07-13T00:00:00Z\""));

        var org = await client.Audit.Orgs.ArchiveAsync("org_x");

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/v1/audit/orgs/org_x/archive", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("2026-07-13T00:00:00Z", (string?)org!["archived_at"]);
    }

    [Fact]
    public async Task UnarchiveAsync_PostsToUnarchivePath()
    {
        var (client, handler) = MockClient(OrgJson);

        var org = await client.Audit.Orgs.UnarchiveAsync("org_x");

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/v1/audit/orgs/org_x/unarchive", handler.Request.RequestUri!.AbsolutePath);
        Assert.Null(org!["archived_at"]);
    }

    [Fact]
    public async Task DeleteAsync_SendsDelete()
    {
        var (client, handler) = MockClient("{\"deleted\":true,\"id\":\"aorg_x\"}");

        var result = await client.Audit.Orgs.DeleteAsync("org_x");

        Assert.Equal(HttpMethod.Delete, handler.Request!.Method);
        Assert.Equal("/v1/audit/orgs/org_x", handler.Request.RequestUri!.AbsolutePath);
        Assert.True((bool?)result!["deleted"]);
    }

    [Fact]
    public async Task DeleteAsync_409_MapsToConflictWithOrgNotDeletable()
    {
        var (client, _) = MockClient("{\"error\":\"org_not_deletable\"}", HttpStatusCode.Conflict);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => client.Audit.Orgs.DeleteAsync("org_x"));

        Assert.Equal("org_not_deletable", ex.ErrorCode);
    }

    [Fact]
    public async Task ListAsync_SendsIncludeArchivedTrueWhenSet()
    {
        var (client, handler) = MockClient($"{{\"orgs\":[{OrgJson}]}}");

        await client.Audit.Orgs.ListAsync(new ListAuditOrgsParams { IncludeArchived = true });

        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("/v1/audit/orgs", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("?include_archived=true", handler.Request.RequestUri.Query);
    }

    [Fact]
    public async Task ListAsync_OmitsIncludeArchivedByDefault()
    {
        var (client, handler) = MockClient($"{{\"orgs\":[{OrgJson}]}}");

        await client.Audit.Orgs.ListAsync();

        Assert.Equal("/v1/audit/orgs", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("", handler.Request.RequestUri.Query);
    }
}
