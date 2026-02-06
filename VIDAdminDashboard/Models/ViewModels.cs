namespace VIDAdminDashboard.Models;

public class DashboardViewModel
{
    public List<Authority> Authorities { get; set; } = new();
    public Authority? SelectedAuthority { get; set; }
    public List<Contract> Contracts { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class CredentialViewModel
{
    public string AuthorityId { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
    public List<Credential> Credentials { get; set; } = new();
    public string? SearchClaimValue { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string? Message { get; set; }
}
