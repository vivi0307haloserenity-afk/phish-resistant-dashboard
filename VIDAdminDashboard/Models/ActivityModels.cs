namespace VIDAdminDashboard.Models;

public class VerifiedIdTransaction
{
    public string Id { get; set; } = string.Empty;
    public string AuthorityDid { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletionDateTime { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientName { get; set; }
    public string? ClientId { get; set; }
}

public class TransactionListResponse
{
    public List<VerifiedIdTransaction> Value { get; set; } = new();
    public string? OdataNextLink { get; set; }
}

public class ActivityDashboardViewModel
{
    public List<Authority> Authorities { get; set; } = new();
    public Authority? SelectedAuthority { get; set; }
    public List<VerifiedIdTransaction> Transactions { get; set; } = new();
    public List<AuditLogEntry> AuditLogs { get; set; } = new();
    public ActivitySummary Summary { get; set; } = new();
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
    public DateTime EndDate { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public bool AuditLogConsentRequired { get; set; } = false;
}

public class ActivitySummary
{
    public int TotalTransactions { get; set; }
    public int IssuanceCount { get; set; }
    public int PresentationCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public Dictionary<string, int> TransactionsByCredentialType { get; set; } = new();
    public Dictionary<string, int> TransactionsByDay { get; set; } = new();
}

// Entra ID Audit Log Models
public class AuditLogEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime ActivityDateTime { get; set; }
    public string ActivityDisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string LoggedByService { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string ResultReason { get; set; } = string.Empty;
    public AuditLogInitiator? InitiatedBy { get; set; }
    public List<AuditLogTargetResource>? TargetResources { get; set; }
}

public class AuditLogInitiator
{
    public AuditLogUser? User { get; set; }
    public AuditLogApp? App { get; set; }
}

public class AuditLogUser
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
}

public class AuditLogApp
{
    public string AppId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ServicePrincipalId { get; set; } = string.Empty;
    public string ServicePrincipalName { get; set; } = string.Empty;
}

public class AuditLogTargetResource
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class AuditLogListResponse
{
    public List<AuditLogEntry> Value { get; set; } = new();
    public string? OdataNextLink { get; set; }
}
