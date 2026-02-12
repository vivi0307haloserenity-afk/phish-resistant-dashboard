namespace VIDAdminDashboard.Models;

// ─── FIDO2 Configuration ───────────────────────────────────────────────

public class Fido2Configuration
{
    public string Id { get; set; } = "fido2";
    public string State { get; set; } = "disabled";
    public bool IsAttestationEnforced { get; set; }
    public bool IsSelfServiceRegistrationAllowed { get; set; }
    public Fido2KeyRestrictions? KeyRestrictions { get; set; }
    public List<AuthenticationMethodTarget> IncludeTargets { get; set; } = new();
    public List<ExcludeTarget> ExcludeTargets { get; set; } = new();
}

public class Fido2KeyRestrictions
{
    public bool IsEnforced { get; set; }
    public string EnforcementType { get; set; } = "block"; // "allow" or "block"
    public List<string> AaGuids { get; set; } = new();
}

// ─── Microsoft Authenticator Configuration ─────────────────────────────

public class MicrosoftAuthenticatorConfiguration
{
    public string Id { get; set; } = "microsoftAuthenticator";
    public string State { get; set; } = "disabled";
    public AuthenticatorFeatureSettings? FeatureSettings { get; set; }
    public List<AuthenticationMethodTarget> IncludeTargets { get; set; } = new();
    public List<ExcludeTarget> ExcludeTargets { get; set; } = new();
}

public class AuthenticatorFeatureSettings
{
    public FeatureState? DisplayAppInformationRequiredState { get; set; }
    public FeatureState? DisplayLocationInformationRequiredState { get; set; }
    public FeatureState? CompanionAppAllowedState { get; set; }
}

public class FeatureState
{
    public string State { get; set; } = "default";
    public List<AuthenticationMethodTarget> IncludeTarget { get; set; } = new();
    public List<ExcludeTarget> ExcludeTarget { get; set; } = new();
}

// ─── Shared Auth Method Types ──────────────────────────────────────────

public class AuthenticationMethodTarget
{
    public string Id { get; set; } = "all_users";
    public string TargetType { get; set; } = "group";
    public bool IsRegistrationRequired { get; set; }
}

public class ExcludeTarget
{
    public string Id { get; set; } = string.Empty;
    public string TargetType { get; set; } = "group";
}

// ─── Authentication Methods Policy ─────────────────────────────────────

public class AuthenticationMethodsPolicy
{
    public string Id { get; set; } = string.Empty;
    public RegistrationEnforcement? RegistrationEnforcement { get; set; }
    public List<AuthenticationMethodConfiguration> AuthenticationMethodConfigurations { get; set; } = new();
}

public class RegistrationEnforcement
{
    public AuthenticationMethodsRegistrationCampaign? AuthenticationMethodsRegistrationCampaign { get; set; }
}

public class AuthenticationMethodsRegistrationCampaign
{
    public string State { get; set; } = "disabled";
    public int SnoozeDurationInDays { get; set; }
    public List<AuthenticationMethodTarget> IncludeTargets { get; set; } = new();
    public List<ExcludeTarget> ExcludeTargets { get; set; } = new();
}

public class AuthenticationMethodConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string State { get; set; } = "disabled";
}

// ─── Tenant Overview ───────────────────────────────────────────────────

public class TenantUserCount
{
    public long TotalUsers { get; set; }
    public long EnabledUsers { get; set; }
}

// ─── Aggregated Registration Reports (from Graph aggregate endpoints) ─

/// <summary>
/// One row from GET /reports/authenticationMethods/usersRegisteredByMethod.
/// Graph returns one entry per auth method with a pre-aggregated user count.
/// </summary>
public class MethodRegistrationCount
{
    public string AuthenticationMethod { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long UserCount { get; set; }
    public double Percentage { get; set; }           // % of total registered users
    public double PercentOfEnabledUsers { get; set; } // % of all enabled tenant users (the real coverage metric)

    /// <summary>True for FIDO2 / passkey methods that are phishing-resistant.</summary>
    public bool IsPhishingResistant => AuthenticationMethod is
        "fido2" or "passKeyDeviceBound" or "passKeyDeviceBoundAuthenticator" or "passKeySynced";

    /// <summary>True for any passwordless method (phishing-resistant + authenticator passwordless + WHfB).</summary>
    public bool IsPasswordless => IsPhishingResistant || AuthenticationMethod is
        "microsoftAuthenticatorPasswordless" or "windowsHelloForBusiness";
}

/// <summary>
/// From GET /reports/authenticationMethods/usersRegisteredByFeature.
/// Graph returns aggregate counts for each feature flag.
/// </summary>
public class FeatureRegistrationSummary
{
    public long TotalUserCount { get; set; }
    public long MfaRegisteredUserCount { get; set; }
    public long MfaCapableUserCount { get; set; }
    public long PasswordlessCapableUserCount { get; set; }
    public long SsprRegisteredUserCount { get; set; }
    public long SsprEnabledUserCount { get; set; }
    public long SsprCapableUserCount { get; set; }
    public List<MethodRegistrationCount> UserRegistrationMethodCounts { get; set; } = new();
}

// ─── Sign-in Activity (Last 24h) ───────────────────────────────────────

/// <summary>
/// Count of sign-ins by authentication method in the last 24 hours.
/// Built from a single /auditLogs/signIns query (top N records).
/// </summary>
public class SignInMethodCount
{
    public string AuthenticationMethod { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public long TotalCount => SuccessCount + FailureCount;
    public double FailureRate => TotalCount > 0 ? Math.Round((double)FailureCount / TotalCount * 100, 1) : 0;
}

/// <summary>
/// Security tier distribution — how users break down by their highest auth tier.
/// Derived from existing usersRegisteredByFeature + usersRegisteredByMethod data.
/// </summary>
public class SecurityTierDistribution
{
    public long Fido2HardwareKey { get; set; }        // fido2 (hardware security keys)
    public long AuthenticatorPasskey { get; set; }     // passKeyDeviceBoundAuthenticator
    public long SyncedPasskey { get; set; }            // passKeySynced
    public long DeviceBoundPasskey { get; set; }       // passKeyDeviceBound (other device-bound)
    public long PasswordlessNonPR { get; set; }        // passwordless minus phishing-resistant
    public long MfaOnly { get; set; }                  // mfaCapable minus passwordless
    public long PasswordOnly { get; set; }             // enabled minus mfaCapable

    public long TotalPhishingResistant => Fido2HardwareKey + AuthenticatorPasskey + SyncedPasskey + DeviceBoundPasskey;
}

/// <summary>
/// A single recent sign-in failure for the "investigate" table.
/// We only fetch a small number of these (top 25 failures).
/// </summary>
public class RecentSignInFailure
{
    public DateTime CreatedDateTime { get; set; }
    public string UserPrincipalName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string AppDisplayName { get; set; } = string.Empty;
    public string AuthenticationMethod { get; set; } = string.Empty;
    public int ErrorCode { get; set; }
    public string FailureReason { get; set; } = string.Empty;
}

// ─── Journey KPIs ──────────────────────────────────────────────────────

public class PasswordlessJourneyKpis
{
    public long TenantTotalUsers { get; set; }
    public long TenantEnabledUsers { get; set; }
    public long PasswordlessCapableUsers { get; set; }
    public long PhishingResistantUsers { get; set; }  // FIDO2 + passkey
    public long MfaRegisteredUsers { get; set; }
    public long MfaCapableUsers { get; set; }
    public long PasswordOnlyUsers { get; set; }        // no passwordless method

    // Computed percentages (against enabled users — the actionable base)
    public double PasswordlessAdoptionPct => TenantEnabledUsers > 0
        ? Math.Round((double)PasswordlessCapableUsers / TenantEnabledUsers * 100, 1) : 0;
    public double PhishingResistantPct => TenantEnabledUsers > 0
        ? Math.Round((double)PhishingResistantUsers / TenantEnabledUsers * 100, 1) : 0;
    public double MfaRegisteredPct => TenantEnabledUsers > 0
        ? Math.Round((double)MfaRegisteredUsers / TenantEnabledUsers * 100, 1) : 0;
    public double PasswordOnlyPct => TenantEnabledUsers > 0
        ? Math.Round((double)PasswordOnlyUsers / TenantEnabledUsers * 100, 1) : 0;

    // Gap analysis
    public long MfaGap => MfaCapableUsers - PasswordlessCapableUsers;
    public double MfaGapPct => TenantEnabledUsers > 0
        ? Math.Round((double)MfaGap / TenantEnabledUsers * 100, 1) : 0;
    public long PhishingResistantGap => PasswordlessCapableUsers - PhishingResistantUsers;
    public double PhishingResistantGapPct => TenantEnabledUsers > 0
        ? Math.Round((double)PhishingResistantGap / TenantEnabledUsers * 100, 1) : 0;

    // Maturity rating for executive summary
    public string MaturityRating => PhishingResistantPct switch
    {
        >= 80 => "Advanced",
        >= 50 => "Progressing",
        >= 20 => "Early",
        _     => "Beginning"
    };
    public string MaturityColor => PhishingResistantPct switch
    {
        >= 80 => "#107c10",
        >= 50 => "#0078d4",
        >= 20 => "#ffb900",
        _     => "#d13438"
    };
}

// ─── View Models ───────────────────────────────────────────────────

// ─── Global Admin Authentication Status ────────────────────────────

public class GlobalAdminAuthStatus
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public bool AccountEnabled { get; set; }
    public List<string> RegisteredMethods { get; set; } = new();

    public bool HasPhishingResistant => RegisteredMethods.Any(m =>
        m is "fido2" or "passKeyDeviceBound" or "windowsHelloForBusiness" or "platformCredential");
    public bool HasPasswordless => HasPhishingResistant || RegisteredMethods.Any(m =>
        m is "microsoftAuthenticatorPasswordless");
    public bool HasMfa => HasPasswordless || RegisteredMethods.Any(m =>
        m is "microsoftAuthenticator" or "softwareOath" or "phone");
    public bool IsPasswordOnly => !HasMfa && RegisteredMethods.Contains("password");

    public string AuthStrength => HasPhishingResistant ? "Phishing Resistant"
        : HasPasswordless ? "Passwordless"
        : HasMfa ? "MFA"
        : "Password Only";
    public string AuthStrengthBadge => HasPhishingResistant ? "bg-primary"
        : HasPasswordless ? "bg-success"
        : HasMfa ? "bg-warning text-dark"
        : "bg-danger";
    public string AuthStrengthColor => HasPhishingResistant ? "#0078d4"
        : HasPasswordless ? "#107c10"
        : HasMfa ? "#ffb900"
        : "#d13438";
}

public class GlobalAdminSummary
{
    public int TotalCount { get; set; }
    public int EnabledCount { get; set; }
    public int PhishingResistantCount { get; set; }
    public int PasswordlessCount { get; set; }
    public int MfaOnlyCount { get; set; }
    public int PasswordOnlyCount { get; set; }
    public List<GlobalAdminAuthStatus> Admins { get; set; } = new();

    public double PhishingResistantPct => EnabledCount > 0
        ? Math.Round((double)PhishingResistantCount / EnabledCount * 100, 1) : 0;
    public double PasswordlessPct => EnabledCount > 0
        ? Math.Round((double)PasswordlessCount / EnabledCount * 100, 1) : 0;
    public double MfaOnlyPct => EnabledCount > 0
        ? Math.Round((double)MfaOnlyCount / EnabledCount * 100, 1) : 0;
    public double PasswordOnlyPct => EnabledCount > 0
        ? Math.Round((double)PasswordOnlyCount / EnabledCount * 100, 1) : 0;

    /// <summary>Overall security posture for Global Admins.</summary>
    public string OverallRating => PasswordOnlyCount > 0 ? "Critical"
        : MfaOnlyCount > 0 ? "Needs Improvement"
        : PhishingResistantCount == EnabledCount ? "Excellent"
        : "Good";
    public string OverallColor => PasswordOnlyCount > 0 ? "#d13438"
        : MfaOnlyCount > 0 ? "#ffb900"
        : PhishingResistantCount == EnabledCount ? "#107c10"
        : "#0078d4";
}

// ─── Phishing-Resistant Enforcement (Conditional Access) ──────────────

public class PhishingResistantEnforcementSummary
{
    public int EnforcingPolicies { get; set; }
    public int ReportOnlyPolicies { get; set; }
    public bool TargetsAllUsers { get; set; }
    public bool TargetsAllApps { get; set; }
    public int TargetedGroupCount { get; set; }
    public int TargetedAppCount { get; set; }
    public int ExcludedGroupCount { get; set; }
    public int ExcludedAppCount { get; set; }

    // Computed helpers for the UI
    public string UserCoverage => TargetsAllUsers ? "All Users" : $"{TargetedGroupCount} group(s)";
    public string AppCoverage => TargetsAllApps ? "All Cloud Apps" : $"{TargetedAppCount} app(s)";
    public int TotalPolicies => EnforcingPolicies + ReportOnlyPolicies;
    public bool HasExclusions => ExcludedGroupCount > 0 || ExcludedAppCount > 0;

    public string OverallVerdict =>
        EnforcingPolicies == 0 ? "Not Enforced"
        : !TargetsAllUsers ? "Partial Coverage"
        : !TargetsAllApps ? "Selective Apps"
        : "Fully Enforced";

    public string VerdictColor =>
        EnforcingPolicies == 0 ? "#d13438"
        : !TargetsAllUsers || !TargetsAllApps ? "#ffb900"
        : "#107c10";

    public string VerdictIcon =>
        EnforcingPolicies == 0 ? "bi-shield-x"
        : !TargetsAllUsers || !TargetsAllApps ? "bi-shield-exclamation"
        : "bi-shield-check";
}

// ─── View Models ───────────────────────────────────────────────────────

public class PasswordlessActivityViewModel
{
    // Journey KPIs (all pre-aggregated, no per-user iteration)
    public PasswordlessJourneyKpis Kpis { get; set; } = new();

    // Global Admin security status (per-user queries OK — small population)
    public GlobalAdminSummary GlobalAdmins { get; set; } = new();

    // Phishing-resistant enforcement via Conditional Access
    public PhishingResistantEnforcementSummary Enforcement { get; set; } = new();

    // Security tier distribution for the doughnut chart
    public SecurityTierDistribution TierDistribution { get; set; } = new();

    // Registration breakdown by method (from Graph aggregate endpoint)
    public List<MethodRegistrationCount> RegistrationsByMethod { get; set; } = new();

    // Last 24h sign-ins by auth method (single API call)
    public List<SignInMethodCount> RecentSignIns { get; set; } = new();

    // Small set of recent failures for investigation
    public List<RecentSignInFailure> RecentFailures { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

public class PasswordlessConfigViewModel
{
    public Fido2Configuration? Fido2Config { get; set; }
    public MicrosoftAuthenticatorConfiguration? AuthenticatorConfig { get; set; }
    public AuthenticationMethodsPolicy? Policy { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
