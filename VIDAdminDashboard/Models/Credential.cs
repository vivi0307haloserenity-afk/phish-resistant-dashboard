namespace VIDAdminDashboard.Models;

public class Credential
{
    public string Id { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IssuedAt { get; set; } = string.Empty;
    public string? IssuedAtTimestamp { get; set; }
}

public class CredentialListResponse
{
    public List<Credential> Value { get; set; } = new();
}

public class CredentialSearchRequest
{
    public string AuthorityId { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}

public class CredentialRevokeRequest
{
    public string AuthorityId { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
}
