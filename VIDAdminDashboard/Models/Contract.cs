namespace VIDAdminDashboard.Models;

public class Contract
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AuthorityId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IssueNotificationEnabled { get; set; }
    public bool AvailableInVcDirectory { get; set; }
    public string ManifestUrl { get; set; } = string.Empty;
    public List<string>? IssueNotificationAllowedToGroupOids { get; set; }
    public RulesModel? Rules { get; set; }
    public List<DisplayModel>? Displays { get; set; }
    public bool AllowOverrideValidityIntervalOnIssuance { get; set; }
}

public class RulesModel
{
    public Attestations? Attestations { get; set; }
    public int ValidityInterval { get; set; }
    public VcType? Vc { get; set; }
    public CustomStatusEndpoint? CustomStatusEndpoint { get; set; }
}

public class Attestations
{
    public List<IdTokenAttestation>? IdTokens { get; set; }
    public List<IdTokenHintAttestation>? IdTokenHints { get; set; }
    public List<VerifiablePresentationAttestation>? Presentations { get; set; }
    public SelfIssuedAttestation? SelfIssued { get; set; }
    public List<AccessTokenAttestation>? AccessTokens { get; set; }
}

public class IdTokenAttestation
{
    public List<ClaimMapping>? Mapping { get; set; }
    public string Configuration { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool Required { get; set; }
}

public class IdTokenHintAttestation
{
    public List<ClaimMapping>? Mapping { get; set; }
    public bool Required { get; set; }
    public List<string>? TrustedIssuers { get; set; }
}

public class VerifiablePresentationAttestation
{
    public List<ClaimMapping>? Mapping { get; set; }
    public string? CredentialType { get; set; }
    public bool Required { get; set; }
    public List<string>? TrustedIssuers { get; set; }
}

public class SelfIssuedAttestation
{
    public List<ClaimMapping>? Mapping { get; set; }
    public bool Required { get; set; }
}

public class AccessTokenAttestation
{
    public List<ClaimMapping>? Mapping { get; set; }
    public bool Required { get; set; }
}

public class ClaimMapping
{
    public string InputClaim { get; set; } = string.Empty;
    public string OutputClaim { get; set; } = string.Empty;
    public bool Indexed { get; set; }
    public bool Required { get; set; }
    public string? Type { get; set; }
}

public class VcType
{
    public List<string> Type { get; set; } = new();
}

public class CustomStatusEndpoint
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class DisplayModel
{
    public string Locale { get; set; } = string.Empty;
    public DisplayCredential? Card { get; set; }
    public DisplayConsent? Consent { get; set; }
    public List<DisplayClaim>? Claims { get; set; }
}

public class DisplayCredential
{
    public string Title { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisplayCredentialLogo? Logo { get; set; }
}

public class DisplayCredentialLogo
{
    public string Uri { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DisplayConsent
{
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

public class DisplayClaim
{
    public string Label { get; set; } = string.Empty;
    public string Claim { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ContractListResponse
{
    public List<Contract> Value { get; set; } = new();
}

public class CreateContractRequest
{
    public string Name { get; set; } = string.Empty;
    public RulesModel? Rules { get; set; }
    public List<DisplayModel>? Displays { get; set; }
}

public class UpdateContractRequest
{
    public RulesModel? Rules { get; set; }
    public List<DisplayModel>? Displays { get; set; }
    public bool? AvailableInVcDirectory { get; set; }
    public bool? AllowOverrideValidityIntervalOnIssuance { get; set; }
    public bool? IssueNotificationEnabled { get; set; }
    public List<string>? IssueNotificationAllowedToGroupOids { get; set; }
}
