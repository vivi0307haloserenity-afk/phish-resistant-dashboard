namespace VIDAdminDashboard.Models;

public class Authority
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DidModel? DidModel { get; set; }
    public KeyVaultMetadata? KeyVaultMetadata { get; set; }
    public bool LinkedDomainsVerified { get; set; }
}

public class DidModel
{
    public string Did { get; set; } = string.Empty;
    public List<string> SigningKeys { get; set; } = new();
    public List<string> RecoveryKeys { get; set; } = new();
    public List<string> UpdateKeys { get; set; } = new();
    public List<string> EncryptionKeys { get; set; } = new();
    public List<string> LinkedDomainUrls { get; set; } = new();
    public string DidDocumentStatus { get; set; } = string.Empty;
}

public class KeyVaultMetadata
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceUrl { get; set; } = string.Empty;
}

public class AuthorityListResponse
{
    public List<Authority> Value { get; set; } = new();
}

public class CreateAuthorityRequest
{
    public string Name { get; set; } = string.Empty;
    public string LinkedDomainUrl { get; set; } = string.Empty;
    public string DidMethod { get; set; } = "web";
    public KeyVaultMetadata? KeyVaultMetadata { get; set; }
}

public class UpdateAuthorityRequest
{
    public string Name { get; set; } = string.Empty;
}

// FaceCheck ARM API Models
public class FaceCheckStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public FaceCheckProperties? Properties { get; set; }
    public bool IsEnabled => Properties != null;
}

public class FaceCheckProperties
{
    public string ProvisioningState { get; set; } = string.Empty;
}

public class EnableFaceCheckRequest
{
    public string Location { get; set; } = string.Empty;
}
