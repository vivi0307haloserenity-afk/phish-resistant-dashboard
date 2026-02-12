using VIDAdminDashboard.Models;

namespace VIDAdminDashboard.Services;

public interface IPasswordlessService
{
    // Authentication Methods Policy
    Task<AuthenticationMethodsPolicy?> GetAuthenticationMethodsPolicyAsync(string accessToken);

    // FIDO2 Configuration
    Task<Fido2Configuration?> GetFido2ConfigurationAsync(string accessToken);
    Task<bool> UpdateFido2ConfigurationAsync(string accessToken, Fido2Configuration config);

    // Microsoft Authenticator Configuration
    Task<MicrosoftAuthenticatorConfiguration?> GetMicrosoftAuthenticatorConfigurationAsync(string accessToken);
    Task<bool> UpdateMicrosoftAuthenticatorConfigurationAsync(string accessToken, MicrosoftAuthenticatorConfiguration config);

    // ─── Aggregate Reports (performant, no per-user iteration) ─────────

    /// <summary>Total + enabled user count from /users?$count=true (single count query).</summary>
    Task<TenantUserCount> GetTenantUserCountAsync(string accessToken);

    /// <summary>Pre-aggregated registration counts by feature from Graph.</summary>
    Task<FeatureRegistrationSummary?> GetRegistrationByFeatureAsync(string accessToken);

    /// <summary>Pre-aggregated registration counts by method from Graph.</summary>
    Task<List<MethodRegistrationCount>> GetRegistrationByMethodAsync(string accessToken);

    /// <summary>Last 24h sign-in counts grouped by authentication method (single API call).</summary>
    Task<List<SignInMethodCount>> GetRecentSignInsByMethodAsync(string accessToken);

    /// <summary>Small set of recent sign-in failures for investigation.</summary>
    Task<List<RecentSignInFailure>> GetRecentSignInFailuresAsync(string accessToken, int top = 25);

    /// <summary>
    /// Gets Global Administrator role members and their registered auth methods.
    /// Per-user queries are acceptable here since GA count is small (&lt;20 recommended).
    /// </summary>
    Task<GlobalAdminSummary> GetGlobalAdminSummaryAsync(string accessToken);

    /// <summary>
    /// Scans Conditional Access policies for those requiring phishing-resistant auth strength.
    /// Returns a summary of enforcing vs report-only policies and their coverage.
    /// </summary>
    Task<PhishingResistantEnforcementSummary> GetPhishingResistantEnforcementAsync(string accessToken);
}
