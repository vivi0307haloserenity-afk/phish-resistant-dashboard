using VIDAdminDashboard.Models;

namespace VIDAdminDashboard.Services;

public interface IVerifiedIdService
{
    // Authority operations
    Task<List<Authority>> GetAuthoritiesAsync(string accessToken);
    Task<Authority?> GetAuthorityAsync(string accessToken, string authorityId);
    Task<Authority?> CreateAuthorityAsync(string accessToken, CreateAuthorityRequest request);
    Task<bool> UpdateAuthorityAsync(string accessToken, string authorityId, UpdateAuthorityRequest request);
    Task<bool> DeleteAuthorityAsync(string accessToken, string authorityId);
    Task<string?> GenerateWellKnownDidConfigurationAsync(string accessToken, string authorityId, string domainUrl);
    Task<string?> GenerateDidDocumentAsync(string accessToken, string authorityId);
    Task<bool> ValidateWellKnownDidConfigurationAsync(string accessToken, string authorityId);
    Task<Authority?> RotateSigningKeyAsync(string accessToken, string authorityId);
    Task<bool> SynchronizeWithDidDocumentAsync(string accessToken, string authorityId);

    // Contract operations
    Task<List<Contract>> GetContractsAsync(string accessToken, string authorityId);
    Task<Contract?> GetContractAsync(string accessToken, string authorityId, string contractId);
    Task<Contract?> CreateContractAsync(string accessToken, string authorityId, CreateContractRequest request);
    Task<bool> UpdateContractAsync(string accessToken, string authorityId, string contractId, UpdateContractRequest request);

    // Credential operations
    Task<Credential?> GetCredentialAsync(string accessToken, string authorityId, string contractId, string credentialId);
    Task<List<Credential>> SearchCredentialsAsync(string accessToken, string authorityId, string contractId, string hashedClaimValue);
    Task<bool> RevokeCredentialAsync(string accessToken, string authorityId, string contractId, string credentialId);

    // Activity/Transactions
    Task<List<VerifiedIdTransaction>> GetTransactionsAsync(string accessToken, string authorityId, DateTime startDate, DateTime endDate, int top = 50);
    Task<List<AuditLogEntry>> GetAuditLogsAsync(string accessToken, DateTime startDate, DateTime endDate, int top = 100);
    Task<string> GetGraphTokenAsync();
    Task<string> GetVerifiedIdAppTokenAsync();

    // FaceCheck ARM API operations (uses delegated user token)
    Task<FaceCheckStatus?> GetFaceCheckStatusAsync(string armAccessToken, string subscriptionId, string resourceGroup, string authorityId);
    Task<bool> EnableFaceCheckAsync(string armAccessToken, string subscriptionId, string resourceGroup, string authorityId, string location);
    Task<bool> DisableFaceCheckAsync(string armAccessToken, string subscriptionId, string resourceGroup, string authorityId);

    // Utility
    string ComputeSearchHash(string contractId, string claimValue);
}
