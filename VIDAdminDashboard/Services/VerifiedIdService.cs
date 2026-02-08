using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Client;
using VIDAdminDashboard.Models;

namespace VIDAdminDashboard.Services;

public class VerifiedIdService : IVerifiedIdService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerifiedIdService> _logger;
    private readonly string _baseUrl;
    private readonly IConfidentialClientApplication _confidentialClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public VerifiedIdService(HttpClient httpClient, IConfiguration configuration, ILogger<VerifiedIdService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["VerifiedId:BaseUrl"] ?? "https://verifiedid.did.msidentity.com";
        
        // Initialize confidential client for client credentials flow (Graph API)
        var tenantId = _configuration["AzureAd:TenantId"];
        var clientId = _configuration["AzureAd:ClientId"];
        var clientSecret = _configuration["AzureAd:ClientSecret"];
        
        _confidentialClient = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();
    }

    public async Task<string> GetGraphTokenAsync()
    {
        try
        {
            var result = await _confidentialClient
                .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                .ExecuteAsync();
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Graph token");
            throw;
        }
    }

    public async Task<string> GetVerifiedIdAppTokenAsync()
    {
        try
        {
            // Use application permissions for VID Admin API
            var result = await _confidentialClient
                .AcquireTokenForClient(new[] { "6a8b4b39-c021-437c-b060-5a14a3fd65f3/.default" })
                .ExecuteAsync();
            _logger.LogInformation("Acquired application token for VID Admin API");
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire VID Admin API application token");
            throw;
        }
    }

    private void SetAuthHeader(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    #region Authority Operations

    public async Task<List<Authority>> GetAuthoritiesAsync(string accessToken)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.GetAsync($"{_baseUrl}/v1.0/verifiableCredentials/authorities");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthorityListResponse>(content, JsonOptions);
                return result?.Value ?? new List<Authority>();
            }
            
            _logger.LogError("Failed to get authorities: {StatusCode}", response.StatusCode);
            return new List<Authority>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authorities");
            return new List<Authority>();
        }
    }

    public async Task<Authority?> GetAuthorityAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.GetAsync($"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Authority>(content, JsonOptions);
            }
            
            _logger.LogError("Failed to get authority {AuthorityId}: {StatusCode}", authorityId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authority {AuthorityId}", authorityId);
            return null;
        }
    }

    public async Task<Authority?> CreateAuthorityAsync(string accessToken, CreateAuthorityRequest request)
    {
        try
        {
            SetAuthHeader(accessToken);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/v1.0/verifiableCredentials/authorities", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Authority>(responseContent, JsonOptions);
            }
            
            _logger.LogError("Failed to create authority: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating authority");
            return null;
        }
    }

    public async Task<bool> UpdateAuthorityAsync(string accessToken, string authorityId, UpdateAuthorityRequest request)
    {
        try
        {
            SetAuthHeader(accessToken);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}")
            {
                Content = content
            };
            
            var response = await _httpClient.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating authority {AuthorityId}", authorityId);
            return false;
        }
    }

    public async Task<bool> DeleteAuthorityAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/beta/verifiableCredentials/authorities/{authorityId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting authority {AuthorityId}", authorityId);
            return false;
        }
    }

    public async Task<string?> GenerateWellKnownDidConfigurationAsync(string accessToken, string authorityId, string domainUrl)
    {
        try
        {
            SetAuthHeader(accessToken);
            var requestBody = new { domainUrl };
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/generateWellknownDidConfiguration", 
                content);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            
            _logger.LogError("Failed to generate well-known DID configuration: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating well-known DID configuration");
            return null;
        }
    }

    public async Task<string?> GenerateDidDocumentAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/generateDidDocument", 
                null);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            
            _logger.LogError("Failed to generate DID document: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DID document");
            return null;
        }
    }

    public async Task<bool> ValidateWellKnownDidConfigurationAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/validateWellKnownDidConfiguration", 
                null);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating well-known DID configuration");
            return false;
        }
    }

    public async Task<Authority?> RotateSigningKeyAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/didInfo/signingKeys/rotate", 
                null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Authority>(content, JsonOptions);
            }
            
            _logger.LogError("Failed to rotate signing key: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating signing key");
            return null;
        }
    }

    public async Task<bool> SynchronizeWithDidDocumentAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/didInfo/synchronizeWithDidDocument", 
                null);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing with DID document");
            return false;
        }
    }

    #endregion

    #region Contract Operations

    public async Task<List<Contract>> GetContractsAsync(string accessToken, string authorityId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.GetAsync($"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ContractListResponse>(content, JsonOptions);
                return result?.Value ?? new List<Contract>();
            }
            
            _logger.LogError("Failed to get contracts: {StatusCode}", response.StatusCode);
            return new List<Contract>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contracts");
            return new List<Contract>();
        }
    }

    public async Task<Contract?> GetContractAsync(string accessToken, string authorityId, string contractId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts/{contractId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Contract>(content, JsonOptions);
            }
            
            _logger.LogError("Failed to get contract {ContractId}: {StatusCode}", contractId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract {ContractId}", contractId);
            return null;
        }
    }

    public async Task<Contract?> CreateContractAsync(string accessToken, string authorityId, CreateContractRequest request)
    {
        try
        {
            SetAuthHeader(accessToken);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts", 
                content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Contract>(responseContent, JsonOptions);
            }
            
            _logger.LogError("Failed to create contract: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract");
            return null;
        }
    }

    public async Task<bool> UpdateContractAsync(string accessToken, string authorityId, string contractId, UpdateContractRequest request)
    {
        try
        {
            SetAuthHeader(accessToken);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, 
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts/{contractId}")
            {
                Content = content
            };
            
            var response = await _httpClient.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract {ContractId}", contractId);
            return false;
        }
    }

    #endregion

    #region Credential Operations

    public async Task<Credential?> GetCredentialAsync(string accessToken, string authorityId, string contractId, string credentialId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts/{contractId}/credentials/{credentialId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Credential>(content, JsonOptions);
            }
            
            _logger.LogError("Failed to get credential {CredentialId}: {StatusCode}", credentialId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credential {CredentialId}", credentialId);
            return null;
        }
    }

    public async Task<List<Credential>> SearchCredentialsAsync(string accessToken, string authorityId, string contractId, string hashedClaimValue)
    {
        try
        {
            SetAuthHeader(accessToken);
            var encodedHash = Uri.EscapeDataString(hashedClaimValue);
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts/{contractId}/credentials?filter=indexclaimhash eq {encodedHash}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CredentialListResponse>(content, JsonOptions);
                return result?.Value ?? new List<Credential>();
            }
            
            _logger.LogError("Failed to search credentials: {StatusCode}", response.StatusCode);
            return new List<Credential>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching credentials");
            return new List<Credential>();
        }
    }

    public async Task<bool> RevokeCredentialAsync(string accessToken, string authorityId, string contractId, string credentialId)
    {
        try
        {
            SetAuthHeader(accessToken);
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/v1.0/verifiableCredentials/authorities/{authorityId}/contracts/{contractId}/credentials/{credentialId}/revoke", 
                null);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credential {CredentialId}", credentialId);
            return false;
        }
    }

    #endregion

    #region Activity/Transactions

    public async Task<List<VerifiedIdTransaction>> GetTransactionsAsync(string accessToken, string authorityId, DateTime startDate, DateTime endDate, int top = 50)
    {
        try
        {
            SetAuthHeader(accessToken);
            
            // Format dates for the API - use ISO 8601 format (yyyy-MM-ddTHH:mm:ssZ)
            var fromDateStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toDateStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            // Use VID Admin API beta endpoint for transactions
            // Endpoint: https://verifiedid.did.msidentity.com/beta/verifiableCredentials/authorities/{authorityId}/transactions
            // Required query parameters: from, to
            var url = $"{_baseUrl}/beta/verifiableCredentials/authorities/{authorityId}/transactions?from={Uri.EscapeDataString(fromDateStr)}&to={Uri.EscapeDataString(toDateStr)}";
            
            _logger.LogInformation("Fetching transactions from VID Admin API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            // Always log the raw response for debugging
            _logger.LogInformation("VID Admin API Response Status: {StatusCode}, Content: {Content}", response.StatusCode, content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TransactionListResponse>(content, JsonOptions);
                var transactions = result?.Value ?? new List<VerifiedIdTransaction>();
                
                // Order and limit results using the computed TransactionDateTime property
                transactions = transactions
                    .OrderByDescending(t => t.TransactionDateTime)
                    .Take(top)
                    .ToList();
                
                _logger.LogInformation("Retrieved {Count} transactions from VID Admin API", transactions.Count);
                return transactions;
            }
            
            _logger.LogWarning("VID Admin API transactions failed ({StatusCode}): {Error}", response.StatusCode, content);
            return new List<VerifiedIdTransaction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for authority {AuthorityId}", authorityId);
            return new List<VerifiedIdTransaction>();
        }
    }

    public async Task<List<AuditLogEntry>> GetAuditLogsAsync(string accessToken, DateTime startDate, DateTime endDate, int top = 100)
    {
        try
        {
            SetAuthHeader(accessToken);
            
            var startDateStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var endDateStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            
            // Filter audit logs for Verified ID service within the date range
            var filter = Uri.EscapeDataString($"loggedByService eq 'Verified ID' and activityDateTime ge {startDateStr} and activityDateTime le {endDateStr}");
            var url = $"https://graph.microsoft.com/v1.0/auditLogs/directoryAudits?$filter={filter}&$top={top}&$orderby=activityDateTime desc";
            
            _logger.LogInformation("Fetching audit logs from: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuditLogListResponse>(content, JsonOptions);
                var auditLogs = result?.Value ?? new List<AuditLogEntry>();
                _logger.LogInformation("Retrieved {Count} audit log entries", auditLogs.Count);
                return auditLogs;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get audit logs: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return new List<AuditLogEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return new List<AuditLogEntry>();
        }
    }

    #endregion

    #region FaceCheck ARM API Operations

    private const string ArmApiVersion = "2024-01-26-preview";
    private const string ArmBaseUrl = "https://management.azure.com";

    public async Task<FaceCheckStatus?> GetFaceCheckStatusAsync(string armAccessToken, string subscriptionId, string resourceGroup, string authorityId)
    {
        try
        {
            // Use delegated user token for ARM (user needs Contributor role)
            SetAuthHeader(armAccessToken);
            
            var url = $"{ArmBaseUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.VerifiedId/authorities/{authorityId}?api-version={ArmApiVersion}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<FaceCheckStatus>(content, JsonOptions);
                return status;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // FaceCheck is not enabled (resource doesn't exist)
                return new FaceCheckStatus { Id = authorityId };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get FaceCheck status: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FaceCheck status for authority {AuthorityId}", authorityId);
            return null;
        }
    }

    public async Task<bool> EnableFaceCheckAsync(string armAccessToken, string subscriptionId, string resourceGroup, string authorityId, string location)
    {
        try
        {
            // Use delegated user token for ARM (user needs Contributor role)
            SetAuthHeader(armAccessToken);
            
            var url = $"{ArmBaseUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.VerifiedId/authorities/{authorityId}?api-version={ArmApiVersion}";
            
            var requestBody = new { location = location };
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = content
            };
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("FaceCheck enabled successfully for authority {AuthorityId}", authorityId);
                return true;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to enable FaceCheck: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling FaceCheck for authority {AuthorityId}", authorityId);
            return false;
        }
    }

    public async Task<bool> DisableFaceCheckAsync(string armAccessToken, string subscriptionId, string resourceGroup, string authorityId)
    {
        try
        {
            // Use delegated user token for ARM (user needs Contributor role)
            SetAuthHeader(armAccessToken);
            
            var url = $"{ArmBaseUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.VerifiedId/authorities/{authorityId}?api-version={ArmApiVersion}";
            
            var response = await _httpClient.DeleteAsync(url);
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation("FaceCheck disabled successfully for authority {AuthorityId}", authorityId);
                return true;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to disable FaceCheck: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling FaceCheck for authority {AuthorityId}", authorityId);
            return false;
        }
    }

    private async Task<string> GetArmTokenAsync()
    {
        try
        {
            var result = await _confidentialClient
                .AcquireTokenForClient(new[] { "https://management.azure.com/.default" })
                .ExecuteAsync();
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire ARM token");
            throw;
        }
    }

    #endregion

    #region Utility Methods

    public string ComputeSearchHash(string contractId, string claimValue)
    {
        var input = contractId + claimValue;
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}
