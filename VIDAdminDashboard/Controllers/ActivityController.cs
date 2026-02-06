using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
public class ActivityController : Controller
{
    private readonly IVerifiedIdService _verifiedIdService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActivityController> _logger;
    
    // Use .default scope for VID Admin API - this works for both user and application tokens
    private const string VerifiedIdScope = "6a8b4b39-c021-437c-b060-5a14a3fd65f3/.default";
    private static readonly string[] VerifiedIdScopes = new[] { VerifiedIdScope };
    
    // Audit logs from Graph API as additional data source
    private static readonly string[] AuditLogScopes = new[] { 
        "https://graph.microsoft.com/AuditLog.Read.All"
    };

    public ActivityController(
        IVerifiedIdService verifiedIdService,
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration,
        ILogger<ActivityController> logger)
    {
        _verifiedIdService = verifiedIdService;
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
        _logger = logger;
    }

    [AuthorizeForScopes(Scopes = new[] { VerifiedIdScope })]
    public async Task<IActionResult> Index(string? authorityId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var viewModel = new ActivityDashboardViewModel
        {
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-3),
            EndDate = endDate ?? DateTime.UtcNow
        };

        try
        {
            // Get Verified ID token for authorities
            var verifiedIdToken = await _tokenAcquisition.GetAccessTokenForUserAsync(VerifiedIdScopes);
            
            // Get all authorities
            viewModel.Authorities = await _verifiedIdService.GetAuthoritiesAsync(verifiedIdToken);
            
            // Select authority
            if (!string.IsNullOrEmpty(authorityId))
            {
                viewModel.SelectedAuthority = viewModel.Authorities.FirstOrDefault(a => a.Id == authorityId);
            }
            else if (viewModel.Authorities.Count > 0)
            {
                viewModel.SelectedAuthority = viewModel.Authorities.First();
            }

            // Get transactions if we have a selected authority
            if (viewModel.SelectedAuthority != null)
            {
                _logger.LogInformation("Fetching transactions for Authority: {AuthorityId}, Start: {Start}, End: {End}", 
                    viewModel.SelectedAuthority.Id, viewModel.StartDate, viewModel.EndDate);
                
                try
                {
                    // Use VID Admin API /beta endpoint to get transactions
                    viewModel.Transactions = await _verifiedIdService.GetTransactionsAsync(
                        verifiedIdToken,
                        viewModel.SelectedAuthority.Id,
                        viewModel.StartDate,
                        viewModel.EndDate,
                        100);
                    
                    _logger.LogInformation("Retrieved {Count} transactions", viewModel.Transactions.Count);
                    
                    // Calculate summary from transactions
                    viewModel.Summary = CalculateSummary(viewModel.Transactions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch transactions from VID Admin API");
                    viewModel.Transactions = new List<VerifiedIdTransaction>();
                    viewModel.ErrorMessage = "Failed to fetch transaction data.";
                }
            }

            // Get Audit Logs for Verified ID service
            try
            {
                _logger.LogInformation("Fetching Verified ID audit logs, Start: {Start}, End: {End}", 
                    viewModel.StartDate, viewModel.EndDate);
                
                var auditLogToken = await _tokenAcquisition.GetAccessTokenForUserAsync(AuditLogScopes);
                
                viewModel.AuditLogs = await _verifiedIdService.GetAuditLogsAsync(
                    auditLogToken,
                    viewModel.StartDate,
                    viewModel.EndDate,
                    100);

                _logger.LogInformation("Retrieved {Count} audit log entries", viewModel.AuditLogs.Count);
                
                // Recalculate summary with both transactions and audit logs
                viewModel.Summary = CalculateSummary(viewModel.Transactions, viewModel.AuditLogs);
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                // User needs to consent to AuditLog.Read.All - set flag to show consent button
                _logger.LogWarning(ex, "Audit log permission not yet granted - user needs to consent to AuditLog.Read.All");
                viewModel.AuditLogConsentRequired = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch audit logs from Graph API");
                // Don't set error message - audit logs are optional, other data may still be available
            }
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            _logger.LogWarning(ex, "User needs to consent or re-authenticate");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading activity dashboard");
            viewModel.ErrorMessage = "Failed to load activity data. Please ensure you have the required permissions.";
        }

        return View(viewModel);
    }

    private ActivitySummary CalculateSummary(List<VerifiedIdTransaction> transactions, List<AuditLogEntry> auditLogs)
    {
        var summary = new ActivitySummary();

        // If we have transactions from the API, use those
        if (transactions.Count > 0)
        {
            summary.TotalTransactions = transactions.Count;
            summary.IssuanceCount = transactions.Count(t => t.Action?.Equals("issuance", StringComparison.OrdinalIgnoreCase) == true);
            summary.PresentationCount = transactions.Count(t => t.Action?.Equals("presentation", StringComparison.OrdinalIgnoreCase) == true);
            summary.SuccessCount = transactions.Count(t => t.Status?.Equals("successful", StringComparison.OrdinalIgnoreCase) == true || 
                                                        t.Status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true);
            summary.FailedCount = transactions.Count(t => t.Status?.Equals("failed", StringComparison.OrdinalIgnoreCase) == true ||
                                                      t.Status?.Equals("failure", StringComparison.OrdinalIgnoreCase) == true);

            // Group by credential type
            summary.TransactionsByCredentialType = transactions
                .Where(t => !string.IsNullOrEmpty(t.CredentialType))
                .GroupBy(t => t.CredentialType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by day
            summary.TransactionsByDay = transactions
                .Where(t => t.CompletionDateTime.HasValue)
                .GroupBy(t => t.CompletionDateTime!.Value.Date.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        // Otherwise, compute summary from audit logs
        else if (auditLogs.Count > 0)
        {
            summary.TotalTransactions = auditLogs.Count;
            
            // Categorize by activity type - common Verified ID activities
            summary.IssuanceCount = auditLogs.Count(a => 
                a.ActivityDisplayName.Contains("Issue", StringComparison.OrdinalIgnoreCase) ||
                a.ActivityDisplayName.Contains("Issuance", StringComparison.OrdinalIgnoreCase));
            
            summary.PresentationCount = auditLogs.Count(a => 
                a.ActivityDisplayName.Contains("Present", StringComparison.OrdinalIgnoreCase) ||
                a.ActivityDisplayName.Contains("Verification", StringComparison.OrdinalIgnoreCase) ||
                a.ActivityDisplayName.Contains("Verify", StringComparison.OrdinalIgnoreCase));
            
            summary.SuccessCount = auditLogs.Count(a => 
                a.Result.Equals("success", StringComparison.OrdinalIgnoreCase));
            
            summary.FailedCount = auditLogs.Count(a => 
                a.Result.Equals("failure", StringComparison.OrdinalIgnoreCase));

            // Group by activity type
            summary.TransactionsByCredentialType = auditLogs
                .GroupBy(a => a.ActivityDisplayName)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by day
            summary.TransactionsByDay = auditLogs
                .GroupBy(a => a.ActivityDateTime.Date.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        return summary;
    }

    private ActivitySummary CalculateSummary(List<VerifiedIdTransaction> transactions)
    {
        return CalculateSummary(transactions, new List<AuditLogEntry>());
    }

    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/AuditLog.Read.All" })]
    public async Task<IActionResult> ConsentAuditLogs()
    {
        try
        {
            // This will trigger consent for AuditLog.Read.All
            await _tokenAcquisition.GetAccessTokenForUserAsync(AuditLogScopes);
        }
        catch
        {
            // Consent flow will redirect, this is expected
        }
        
        return RedirectToAction(nameof(Index));
    }
}
