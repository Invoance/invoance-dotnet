using System.Text.Json.Serialization;

namespace Invoance.Models;

/// <summary>Public issuer/organization metadata attached to verify responses.</summary>
public sealed class OrganizationPublic
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("issuer_name")] public string IssuerName { get; set; } = "";
    [JsonPropertyName("primary_domain")] public string PrimaryDomain { get; set; } = "";
    [JsonPropertyName("domain_verified")] public bool DomainVerified { get; set; }
    [JsonPropertyName("domain_verified_at")] public string? DomainVerifiedAt { get; set; }
    [JsonPropertyName("logo_url")] public string? LogoUrl { get; set; }
}
