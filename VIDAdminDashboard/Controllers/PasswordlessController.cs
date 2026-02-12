using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
public class PasswordlessController : Controller
{
    private readonly IPasswordlessService _passwordlessService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<PasswordlessController> _logger;
    private readonly IMemoryCache _cache;

    private static readonly string[] ReportScopes = new[]
    {
        "https://graph.microsoft.com/Reports.Read.All",
        "https://graph.microsoft.com/AuditLog.Read.All",
        "https://graph.microsoft.com/User.Read.All",
        "https://graph.microsoft.com/Directory.Read.All",
        "https://graph.microsoft.com/UserAuthenticationMethod.Read.All",
        "https://graph.microsoft.com/Policy.Read.All"
    };

    private static readonly string[] PolicyReadScopes = new[]
    {
        "https://graph.microsoft.com/Policy.Read.All"
    };

    private static readonly string[] PolicyWriteScopes = new[]
    {
        "https://graph.microsoft.com/Policy.ReadWrite.AuthenticationMethod"
    };

    public PasswordlessController(
        IPasswordlessService passwordlessService,
        ITokenAcquisition tokenAcquisition,
        ILogger<PasswordlessController> logger,
        IMemoryCache cache)
    {
        _passwordlessService = passwordlessService;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
        _cache = cache;
    }

    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Reports.Read.All", "https://graph.microsoft.com/AuditLog.Read.All", "https://graph.microsoft.com/User.Read.All", "https://graph.microsoft.com/Directory.Read.All", "https://graph.microsoft.com/UserAuthenticationMethod.Read.All" })]
    public async Task<IActionResult> Activity()
    {
        var viewModel = new PasswordlessActivityViewModel();

        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(ReportScopes);

            // All calls use pre-aggregated Graph endpoints or small queries.
            // None iterate individual user records — safe for 1M+ tenants.
            var tenantCountTask = _passwordlessService.GetTenantUserCountAsync(token);
            var featureTask = _passwordlessService.GetRegistrationByFeatureAsync(token);
            var methodTask = _passwordlessService.GetRegistrationByMethodAsync(token);
            var failuresTask = _passwordlessService.GetRecentSignInFailuresAsync(token, 25);
            var globalAdminTask = _passwordlessService.GetGlobalAdminSummaryAsync(token);
            var recentSignInsTask = _passwordlessService.GetRecentSignInsByMethodAsync(token);
            var enforcementTask = _passwordlessService.GetPhishingResistantEnforcementAsync(token);

            await Task.WhenAll(tenantCountTask, featureTask, methodTask, failuresTask, globalAdminTask, recentSignInsTask, enforcementTask);

            var tenantCount = await tenantCountTask;
            var feature = await featureTask;
            var methods = await methodTask;

            // Build journey KPIs from the aggregated data
            // Individual PR method counts
            var fido2HwCount = methods.FirstOrDefault(m => m.AuthenticationMethod == "fido2")?.UserCount ?? 0;
            var authPasskeyCount = methods.FirstOrDefault(m => m.AuthenticationMethod == "passKeyDeviceBoundAuthenticator")?.UserCount ?? 0;
            var syncedPasskeyCount = methods.FirstOrDefault(m => m.AuthenticationMethod == "passKeySynced")?.UserCount ?? 0;
            var deviceBoundPasskeyCount = methods.FirstOrDefault(m => m.AuthenticationMethod == "passKeyDeviceBound")?.UserCount ?? 0;
            var rawPrSum = fido2HwCount + authPasskeyCount + syncedPasskeyCount + deviceBoundPasskeyCount;

            // Cap at passwordlessCapable (which is deduplicated by Graph) to avoid
            // double-counting users who registered multiple PR methods.
            // PR ⊆ passwordless, so unique PR users ≤ passwordlessCapable.
            var passwordlessCapable = feature?.PasswordlessCapableUserCount ?? 0;
            var totalPrUsers = passwordlessCapable > 0
                ? Math.Min(rawPrSum, passwordlessCapable)
                : rawPrSum;

            viewModel.Kpis = new PasswordlessJourneyKpis
            {
                TenantTotalUsers = tenantCount.TotalUsers,
                TenantEnabledUsers = tenantCount.EnabledUsers,
                PasswordlessCapableUsers = passwordlessCapable,
                PhishingResistantUsers = totalPrUsers,
                MfaRegisteredUsers = feature?.MfaRegisteredUserCount > 0
                    ? feature.MfaRegisteredUserCount
                    : feature?.MfaCapableUserCount ?? 0,
                MfaCapableUsers = feature?.MfaCapableUserCount ?? 0,
                PasswordOnlyUsers = tenantCount.EnabledUsers - passwordlessCapable
            };

            // Compute % of enabled users for each method (the real coverage metric)
            if (tenantCount.EnabledUsers > 0)
            {
                foreach (var m in methods)
                {
                    m.PercentOfEnabledUsers = Math.Round(
                        (double)m.UserCount / tenantCount.EnabledUsers * 100, 1);
                }
            }

            viewModel.RegistrationsByMethod = methods;
            viewModel.RecentFailures = await failuresTask;
            viewModel.GlobalAdmins = await globalAdminTask;
            viewModel.RecentSignIns = await recentSignInsTask;
            viewModel.Enforcement = await enforcementTask;

            // Build security tier distribution — use capped PR count for consistency
            var mfaCapable = feature?.MfaCapableUserCount ?? 0;

            viewModel.TierDistribution = new SecurityTierDistribution
            {
                Fido2HardwareKey = fido2HwCount,
                AuthenticatorPasskey = authPasskeyCount,
                SyncedPasskey = syncedPasskeyCount,
                DeviceBoundPasskey = deviceBoundPasskeyCount,
                PasswordlessNonPR = Math.Max(0, passwordlessCapable - totalPrUsers),
                MfaOnly = Math.Max(0, mfaCapable - passwordlessCapable),
                PasswordOnly = Math.Max(0, tenantCount.EnabledUsers - mfaCapable)
            };
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            _logger.LogWarning(ex, "User needs to consent or re-authenticate for reports");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading passwordless activity dashboard");
            viewModel.ErrorMessage = "Failed to load activity data. Please ensure you have Reports.Read.All, AuditLog.Read.All, and User.Read.All permissions.";
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForceRefresh()
    {
        // Clear all passwordless dashboard cache entries
        var cacheKeys = new[]
        {
            "tenant:userCount", "report:regByFeature", "report:regByMethod",
            "report:recentSignIns24h", "report:recentFailures:25",
            "report:globalAdmins", "report:prEnforcement",
            "policy:authMethods", "policy:fido2", "policy:authenticator"
        };

        foreach (var key in cacheKeys)
            _cache.Remove(key);

        _logger.LogInformation("Dashboard cache cleared by user request");
        return RedirectToAction("Activity");
    }

    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Policy.Read.All" })]
    public async Task<IActionResult> Configuration()
    {
        var viewModel = new PasswordlessConfigViewModel();

        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(PolicyReadScopes);

            var fido2Task = _passwordlessService.GetFido2ConfigurationAsync(token);
            var authTask = _passwordlessService.GetMicrosoftAuthenticatorConfigurationAsync(token);
            var policyTask = _passwordlessService.GetAuthenticationMethodsPolicyAsync(token);

            await Task.WhenAll(fido2Task, authTask, policyTask);

            viewModel.Fido2Config = await fido2Task;
            viewModel.AuthenticatorConfig = await authTask;
            viewModel.Policy = await policyTask;
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            _logger.LogWarning(ex, "User needs to consent or re-authenticate for policy access");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading passwordless configuration");
            viewModel.ErrorMessage = "Failed to load configuration. Please ensure you have Policy.Read.All permission.";
        }

        return View(viewModel);
    }
}
