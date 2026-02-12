using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using VIDAdminDashboard.Models;

namespace VIDAdminDashboard.Services;

public class PasswordlessService : IPasswordlessService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PasswordlessService> _logger;
    private const string GraphBetaUrl = "https://graph.microsoft.com/beta";
    private const string GraphV1Url = "https://graph.microsoft.com/v1.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Cache durations — aggregate reports are computed daily by Graph, no need
    // to re-fetch on every page load.
    private static readonly TimeSpan RegistrationCacheTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SignInTrendCacheTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan TenantCountCacheTtl = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan PolicyCacheTtl = TimeSpan.FromMinutes(10);

    public PasswordlessService(HttpClient httpClient, IMemoryCache cache, ILogger<PasswordlessService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    private void SetAuthHeader(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // ════════════════════════════════════════════════════════════════════
    //  POLICY / CONFIGURATION ENDPOINTS (unchanged except caching)
    // ════════════════════════════════════════════════════════════════════

    public async Task<AuthenticationMethodsPolicy?> GetAuthenticationMethodsPolicyAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("policy:authMethods", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PolicyCacheTtl;
            try
            {
                SetAuthHeader(accessToken);
                var response = await _httpClient.GetAsync($"{GraphBetaUrl}/policies/authenticationMethodsPolicy");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get auth methods policy: {Status}", response.StatusCode);
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                    return null;
                }
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AuthenticationMethodsPolicy>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching authentication methods policy");
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return null;
            }
        });
    }

    public async Task<Fido2Configuration?> GetFido2ConfigurationAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("policy:fido2", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PolicyCacheTtl;
            try
            {
                SetAuthHeader(accessToken);
                var response = await _httpClient.GetAsync(
                    $"{GraphBetaUrl}/policies/authenticationMethodsPolicy/authenticationMethodConfigurations/fido2");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get FIDO2 config: {Status}", response.StatusCode);
                    return null;
                }
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Fido2Configuration>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching FIDO2 configuration");
                return null;
            }
        });
    }

    public async Task<bool> UpdateFido2ConfigurationAsync(string accessToken, Fido2Configuration config)
    {
        try
        {
            SetAuthHeader(accessToken);
            var content = new StringContent(
                JsonSerializer.Serialize(config, JsonOptions), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{GraphBetaUrl}/policies/authenticationMethodsPolicy/authenticationMethodConfigurations/fido2")
            { Content = content };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update FIDO2 config: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _cache.Remove("policy:fido2");
            _logger.LogInformation("FIDO2 configuration updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating FIDO2 configuration");
            return false;
        }
    }

    public async Task<MicrosoftAuthenticatorConfiguration?> GetMicrosoftAuthenticatorConfigurationAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("policy:authenticator", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PolicyCacheTtl;
            try
            {
                SetAuthHeader(accessToken);
                var response = await _httpClient.GetAsync(
                    $"{GraphBetaUrl}/policies/authenticationMethodsPolicy/authenticationMethodConfigurations/microsoftAuthenticator");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get Authenticator config: {Status}", response.StatusCode);
                    return null;
                }
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MicrosoftAuthenticatorConfiguration>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Microsoft Authenticator configuration");
                return null;
            }
        });
    }

    public async Task<bool> UpdateMicrosoftAuthenticatorConfigurationAsync(string accessToken, MicrosoftAuthenticatorConfiguration config)
    {
        try
        {
            SetAuthHeader(accessToken);
            var content = new StringContent(
                JsonSerializer.Serialize(config, JsonOptions), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{GraphBetaUrl}/policies/authenticationMethodsPolicy/authenticationMethodConfigurations/microsoftAuthenticator")
            { Content = content };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update Authenticator config: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _cache.Remove("policy:authenticator");
            _logger.LogInformation("Microsoft Authenticator configuration updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Microsoft Authenticator configuration");
            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  AGGREGATE REPORT ENDPOINTS (performant — no per-user iteration)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets total + enabled user count via /users/$count with ConsistencyLevel:eventual.
    /// This returns only a count header, not user records — safe for 1M+ tenants.
    /// </summary>
    public async Task<TenantUserCount> GetTenantUserCountAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("tenant:userCount", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TenantCountCacheTtl;
            var result = new TenantUserCount();

            try
            {
                SetAuthHeader(accessToken);

                // Total users (all user objects)
                result.TotalUsers = await GetCountAsync(
                    $"{GraphV1Url}/users/$count");

                // Enabled users (accounts that can sign in)
                result.EnabledUsers = await GetCountAsync(
                    $"{GraphV1Url}/users/$count?$filter=accountEnabled eq true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tenant user counts");
            }

            return result;
        }) ?? new TenantUserCount();
    }

    /// <summary>
    /// Pre-aggregated registration summary by feature.
    /// Endpoint: GET /reports/authenticationMethods/usersRegisteredByFeature
    /// Returns one JSON object with aggregate counts — no pagination, no per-user data.
    /// </summary>
    public async Task<FeatureRegistrationSummary?> GetRegistrationByFeatureAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("report:regByFeature", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = RegistrationCacheTtl;
            try
            {
                SetAuthHeader(accessToken);
                var response = await _httpClient.GetAsync(
                    $"{GraphBetaUrl}/reports/authenticationMethods/usersRegisteredByFeature");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get registration by feature: {Status}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var summary = new FeatureRegistrationSummary();

                if (root.TryGetProperty("totalUserCount", out var total))
                    summary.TotalUserCount = total.GetInt64();
                if (root.TryGetProperty("userRegistrationFeatureCounts", out var features) &&
                    features.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in features.EnumerateArray())
                    {
                        var feature = f.TryGetProperty("feature", out var feat) ? feat.GetString() ?? "" : "";
                        var count = f.TryGetProperty("userCount", out var c) ? c.GetInt64() : 0;

                        switch (feature)
                        {
                            case "mfaCapable": summary.MfaCapableUserCount = count; break;
                            case "mfaRegistered": summary.MfaRegisteredUserCount = count; break;
                            case "passwordlessCapable": summary.PasswordlessCapableUserCount = count; break;
                            case "ssprCapable": summary.SsprCapableUserCount = count; break;
                            case "ssprEnabled": summary.SsprEnabledUserCount = count; break;
                            case "ssprRegistered": summary.SsprRegisteredUserCount = count; break;
                        }
                    }
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching registration by feature");
                return null;
            }
        });
    }

    /// <summary>
    /// Pre-aggregated registration counts by method.
    /// Endpoint: GET /reports/authenticationMethods/usersRegisteredByMethod
    /// Returns one row per method with a count — no pagination needed.
    /// </summary>
    public async Task<List<MethodRegistrationCount>> GetRegistrationByMethodAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("report:regByMethod", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = RegistrationCacheTtl;
            var results = new List<MethodRegistrationCount>();

            try
            {
                SetAuthHeader(accessToken);
                var response = await _httpClient.GetAsync(
                    $"{GraphBetaUrl}/reports/authenticationMethods/usersRegisteredByMethod");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get registration by method: {Status}", response.StatusCode);
                    return results;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                long totalCount = 0;
                if (root.TryGetProperty("totalUserCount", out var total))
                    totalCount = total.GetInt64();

                if (root.TryGetProperty("userRegistrationMethodCounts", out var methods) &&
                    methods.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in methods.EnumerateArray())
                    {
                        var method = m.TryGetProperty("authenticationMethod", out var am) ? am.GetString() ?? "" : "";
                        var count = m.TryGetProperty("userCount", out var c) ? c.GetInt64() : 0;

                        results.Add(new MethodRegistrationCount
                        {
                            AuthenticationMethod = method,
                            DisplayName = GetMethodDisplayName(method),
                            UserCount = count,
                            Percentage = totalCount > 0 ? Math.Round((double)count / totalCount * 100, 1) : 0
                        });
                    }
                }

                // Sort descending by user count for better chart readability
                results.Sort((a, b) => b.UserCount.CompareTo(a.UserCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching registration by method");
            }

            return results;
        }) ?? new List<MethodRegistrationCount>();
    }

    /// <summary>
    /// Gets last 24h sign-in counts grouped by authentication method.
    /// Single API call fetching top 200 recent sign-ins, then aggregated in-memory.
    /// Much faster than the previous multi-day $count approach.
    /// </summary>
    public async Task<List<SignInMethodCount>> GetRecentSignInsByMethodAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("report:recentSignIns24h", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SignInTrendCacheTtl;
            var results = new Dictionary<string, SignInMethodCount>(StringComparer.OrdinalIgnoreCase);

            try
            {
                SetAuthHeader(accessToken);
                var since = DateTime.UtcNow.AddHours(-24).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var url = $"{GraphBetaUrl}/auditLogs/signIns" +
                          $"?$filter=createdDateTime ge {since}" +
                          $"&$top=200";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("ConsistencyLevel", "eventual");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get recent sign-ins: {Status} - {Error}", response.StatusCode, errorBody);
                    return new List<SignInMethodCount>();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("value", out var signIns) && signIns.ValueKind == JsonValueKind.Array)
                {
                    foreach (var signIn in signIns.EnumerateArray())
                    {
                        // Extract auth method and check for succeeded detail
                        var methodName = "Unknown";
                        var hasSucceededDetail = false;
                        if (signIn.TryGetProperty("authenticationDetails", out var details) &&
                            details.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var detail in details.EnumerateArray())
                            {
                                if (detail.TryGetProperty("authenticationMethod", out var am))
                                {
                                    var m = am.GetString() ?? "";
                                    if (!string.IsNullOrEmpty(m) && methodName == "Unknown")
                                    {
                                        methodName = m;
                                    }
                                }
                                // Check if any detail shows succeeded
                                if (detail.TryGetProperty("succeeded", out var succeeded) &&
                                    succeeded.ValueKind == JsonValueKind.True)
                                {
                                    hasSucceededDetail = true;
                                }
                            }
                        }

                        // Determine success/failure
                        // Error code 50140 (Interrupted) is treated as success
                        // when an authentication detail shows succeeded = true
                        var isSuccess = false;
                        if (signIn.TryGetProperty("status", out var status) &&
                            status.TryGetProperty("errorCode", out var errCode))
                        {
                            var code = errCode.GetInt32();
                            isSuccess = code == 0 || (code == 50140 && hasSucceededDetail);
                        }

                        if (!results.ContainsKey(methodName))
                        {
                            results[methodName] = new SignInMethodCount
                            {
                                AuthenticationMethod = methodName,
                                DisplayName = methodName
                            };
                        }

                        if (isSuccess)
                            results[methodName].SuccessCount++;
                        else
                            results[methodName].FailureCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent sign-ins by method");
            }

            return results.Values
                .Where(r => !r.AuthenticationMethod.Equals("Previously satisfied", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.TotalCount)
                .ToList();
        }) ?? new List<SignInMethodCount>();
    }

    /// <summary>
    /// Gets the most recent sign-in failures (small set) for tactical investigation.
    /// Only downloads top N failure records — not the full audit log.
    /// </summary>
    public async Task<List<RecentSignInFailure>> GetRecentSignInFailuresAsync(string accessToken, int top = 25)
    {
        return await _cache.GetOrCreateAsync($"report:recentFailures:{top}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SignInTrendCacheTtl;
            var results = new List<RecentSignInFailure>();

            try
            {
                SetAuthHeader(accessToken);
                var url = $"{GraphBetaUrl}/auditLogs/signIns" +
                          $"?$filter=status/errorCode ne 0" +
                          $"&$top={top}" +
                          $"&$orderby=createdDateTime desc" +
                          $"&$select=createdDateTime,userPrincipalName,userDisplayName,appDisplayName,status,authenticationDetails";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get recent failures: {Status}", response.StatusCode);
                    return results;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("value", out var valueArray))
                {
                    foreach (var item in valueArray.EnumerateArray())
                    {
                        var failure = new RecentSignInFailure();

                        if (item.TryGetProperty("createdDateTime", out var dt) && DateTime.TryParse(dt.GetString(), out var parsed))
                            failure.CreatedDateTime = parsed;
                        if (item.TryGetProperty("userPrincipalName", out var upn))
                            failure.UserPrincipalName = upn.GetString() ?? "";
                        if (item.TryGetProperty("userDisplayName", out var name))
                            failure.UserDisplayName = name.GetString() ?? "";
                        if (item.TryGetProperty("appDisplayName", out var app))
                            failure.AppDisplayName = app.GetString() ?? "";

                        if (item.TryGetProperty("status", out var status))
                        {
                            if (status.TryGetProperty("errorCode", out var code))
                                failure.ErrorCode = code.GetInt32();
                            if (status.TryGetProperty("failureReason", out var reason))
                                failure.FailureReason = reason.GetString() ?? "";
                        }

                        // Extract primary auth method from authenticationDetails array
                        if (item.TryGetProperty("authenticationDetails", out var authDetails) &&
                            authDetails.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var detail in authDetails.EnumerateArray())
                            {
                                if (detail.TryGetProperty("authenticationMethod", out var method))
                                {
                                    failure.AuthenticationMethod = method.GetString() ?? "";
                                    break;
                                }
                            }
                        }

                        results.Add(failure);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent sign-in failures");
            }

            return results;
        }) ?? new List<RecentSignInFailure>();
    }

    // ════════════════════════════════════════════════════════════════════
    //  GLOBAL ADMIN AUTH STATUS (per-user — population is always small)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all Global Administrator role members and their registered auth methods.
    /// Global Admin count should be small (&lt;20 recommended), so per-user queries are fine.
    /// Uses the well-known role template ID for Global Administrator.
    /// </summary>
    private const string GlobalAdminRoleTemplateId = "62e90394-69f5-4237-9190-012177145e10";
    private static readonly TimeSpan GlobalAdminCacheTtl = TimeSpan.FromMinutes(30);

    public async Task<GlobalAdminSummary> GetGlobalAdminSummaryAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("report:globalAdmins", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = GlobalAdminCacheTtl;
            var summary = new GlobalAdminSummary();

            try
            {
                SetAuthHeader(accessToken);

                // Step 1: Get all active Global Admin role assignments (catches permanent + PIM-active + group-based)
                var assignmentsUrl = $"{GraphV1Url}/roleManagement/directory/roleAssignments" +
                                    $"?$filter=roleDefinitionId eq '{GlobalAdminRoleTemplateId}'" +
                                    "&$select=principalId,directoryScopeId";
                var request = new HttpRequestMessage(HttpMethod.Get, assignmentsUrl);
                request.Headers.Add("ConsistencyLevel", "eventual");
                var assignmentsResponse = await _httpClient.SendAsync(request);

                if (!assignmentsResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get Global Admin role assignments: {Status}", assignmentsResponse.StatusCode);
                    return summary;
                }

                var assignmentsJson = await assignmentsResponse.Content.ReadAsStringAsync();
                using var assignmentsDoc = JsonDocument.Parse(assignmentsJson);

                if (!assignmentsDoc.RootElement.TryGetProperty("value", out var assignments) ||
                    assignments.ValueKind != JsonValueKind.Array)
                    return summary;

                // Collect all unique principal IDs
                var principalIds = new HashSet<string>();
                foreach (var assignment in assignments.EnumerateArray())
                {
                    if (assignment.TryGetProperty("principalId", out var pid))
                    {
                        var id = pid.GetString();
                        if (!string.IsNullOrEmpty(id))
                            principalIds.Add(id);
                    }
                }

                // Step 2: Resolve each principal — could be a user or a group
                var userMap = new Dictionary<string, GlobalAdminAuthStatus>();

                var resolveTasks = principalIds.Select(async principalId =>
                {
                    try
                    {
                        // Try to get as user first
                        var userUrl = $"{GraphV1Url}/users/{principalId}?$select=id,displayName,userPrincipalName,accountEnabled";
                        var userResponse = await _httpClient.GetAsync(userUrl);

                        if (userResponse.IsSuccessStatusCode)
                        {
                            var userJson = await userResponse.Content.ReadAsStringAsync();
                            using var userDoc = JsonDocument.Parse(userJson);
                            var user = userDoc.RootElement;

                            var admin = new GlobalAdminAuthStatus
                            {
                                UserId = user.TryGetProperty("id", out var uid) ? uid.GetString() ?? "" : "",
                                DisplayName = user.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
                                UserPrincipalName = user.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() ?? "" : "",
                                AccountEnabled = user.TryGetProperty("accountEnabled", out var ae) && ae.GetBoolean()
                            };

                            lock (userMap)
                            {
                                userMap.TryAdd(admin.UserId, admin);
                            }
                            return;
                        }

                        // If not a user, it might be a group — get transitive members
                        var groupMembersUrl = $"{GraphV1Url}/groups/{principalId}/transitiveMembers/microsoft.graph.user" +
                                             "?$select=id,displayName,userPrincipalName,accountEnabled";
                        var groupResponse = await _httpClient.GetAsync(groupMembersUrl);

                        if (groupResponse.IsSuccessStatusCode)
                        {
                            var groupJson = await groupResponse.Content.ReadAsStringAsync();
                            using var groupDoc = JsonDocument.Parse(groupJson);

                            if (groupDoc.RootElement.TryGetProperty("value", out var groupMembers) &&
                                groupMembers.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var member in groupMembers.EnumerateArray())
                                {
                                    var admin = new GlobalAdminAuthStatus
                                    {
                                        UserId = member.TryGetProperty("id", out var mid) ? mid.GetString() ?? "" : "",
                                        DisplayName = member.TryGetProperty("displayName", out var mdn) ? mdn.GetString() ?? "" : "",
                                        UserPrincipalName = member.TryGetProperty("userPrincipalName", out var mupn) ? mupn.GetString() ?? "" : "",
                                        AccountEnabled = member.TryGetProperty("accountEnabled", out var mae) && mae.GetBoolean()
                                    };

                                    lock (userMap)
                                    {
                                        userMap.TryAdd(admin.UserId, admin);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error resolving principal {PrincipalId}", principalId);
                    }
                });

                await Task.WhenAll(resolveTasks);

                var adminList = userMap.Values.ToList();

                // Step 3: Get auth methods for each admin (in parallel — small list)
                var authTasks = adminList.Select(async admin =>
                {
                    try
                    {
                        var methodsUrl = $"{GraphV1Url}/users/{admin.UserId}/authentication/methods";
                        var methodsResponse = await _httpClient.GetAsync(methodsUrl);

                        if (!methodsResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Failed to get auth methods for {User}: {Status}",
                                admin.UserPrincipalName, methodsResponse.StatusCode);
                            return;
                        }

                        var methodsJson = await methodsResponse.Content.ReadAsStringAsync();
                        using var methodsDoc = JsonDocument.Parse(methodsJson);

                        if (methodsDoc.RootElement.TryGetProperty("value", out var methodsArray) &&
                            methodsArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var method in methodsArray.EnumerateArray())
                            {
                                var methodType = method.TryGetProperty("@odata.type", out var mt)
                                    ? mt.GetString() ?? ""
                                    : "";

                                var mapped = MapAuthMethodType(methodType);
                                if (!string.IsNullOrEmpty(mapped) && !admin.RegisteredMethods.Contains(mapped))
                                    admin.RegisteredMethods.Add(mapped);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error fetching auth methods for {User}", admin.UserPrincipalName);
                    }
                });

                await Task.WhenAll(authTasks);

                // Step 4: Build summary
                summary.TotalCount = adminList.Count;
                summary.EnabledCount = adminList.Count(a => a.AccountEnabled);
                summary.PhishingResistantCount = adminList.Count(a => a.AccountEnabled && a.HasPhishingResistant);
                summary.PasswordlessCount = adminList.Count(a => a.AccountEnabled && a.HasPasswordless);
                summary.MfaOnlyCount = adminList.Count(a => a.AccountEnabled && a.HasMfa && !a.HasPasswordless);
                summary.PasswordOnlyCount = adminList.Count(a => a.AccountEnabled && a.IsPasswordOnly);
                summary.Admins = adminList.OrderBy(a => a.AuthStrength switch
                {
                    "Password Only" => 0,   // most critical first
                    "MFA" => 1,
                    "Passwordless" => 2,
                    _ => 3
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Global Admin auth summary");
            }

            return summary;
        }) ?? new GlobalAdminSummary();
    }

    /// <summary>Maps @odata.type from authentication/methods response to a short method name.</summary>
    private static string MapAuthMethodType(string odataType) => odataType switch
    {
        "#microsoft.graph.fido2AuthenticationMethod" => "fido2",
        "#microsoft.graph.windowsHelloForBusinessAuthenticationMethod" => "windowsHelloForBusiness",
        "#microsoft.graph.microsoftAuthenticatorAuthenticationMethod" => "microsoftAuthenticator",
        "#microsoft.graph.passwordlessMicrosoftAuthenticatorAuthenticationMethod" => "microsoftAuthenticatorPasswordless",
        "#microsoft.graph.phoneAuthenticationMethod" => "phone",
        "#microsoft.graph.softwareOathAuthenticationMethod" => "softwareOath",
        "#microsoft.graph.emailAuthenticationMethod" => "email",
        "#microsoft.graph.temporaryAccessPassAuthenticationMethod" => "temporaryAccessPass",
        "#microsoft.graph.passwordAuthenticationMethod" => "password",
        "#microsoft.graph.platformCredentialAuthenticationMethod" => "platformCredential",
        _ => ""
    };

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Performs a $count query against /users/$count with ConsistencyLevel:eventual.
    /// Returns just a long integer — no record bodies are transferred.
    /// </summary>
    private async Task<long> GetCountAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("ConsistencyLevel", "eventual");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Count query failed ({Status}): {Url}", response.StatusCode, url);
            return 0;
        }

        var text = await response.Content.ReadAsStringAsync();
        return long.TryParse(text.Trim(), out var count) ? count : 0;
    }

    private static string GetMethodDisplayName(string method) => method switch
    {
        "fido2" => "FIDO2 Security Key",
        "windowsHelloForBusiness" => "Windows Hello for Business",
        "microsoftAuthenticatorPush" => "Authenticator Push",
        "microsoftAuthenticatorPasswordless" => "Authenticator Passwordless",
        "softwareOneTimePasscode" => "Software TOTP",
        "mobilePhone" => "SMS / Phone",
        "email" => "Email",
        "temporaryAccessPass" => "Temporary Access Pass",
        "passKeyDeviceBound" => "Device-bound Passkey",
        "passKeyDeviceBoundAuthenticator" => "Authenticator Passkey",
        "passKeySynced" => "Synced Passkey",
        "password" => "Password",
        _ => method
    };

    // ════════════════════════════════════════════════════════════════════
    //  PHISHING-RESISTANT ENFORCEMENT (Conditional Access)
    // ════════════════════════════════════════════════════════════════════

    private static readonly TimeSpan EnforcementCacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Scans all Conditional Access policies for those that require a phishing-resistant
    /// authentication strength. Identifies enforcing vs report-only, user/app coverage.
    /// Uses GET /identity/conditionalAccess/policies (requires Policy.Read.All).
    /// </summary>
    public async Task<PhishingResistantEnforcementSummary> GetPhishingResistantEnforcementAsync(string accessToken)
    {
        return await _cache.GetOrCreateAsync("report:prEnforcement", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = EnforcementCacheTtl;
            var summary = new PhishingResistantEnforcementSummary();

            try
            {
                SetAuthHeader(accessToken);

                // Step 1: Get auth strength policies to find which ones are phishing-resistant
                var prStrengthIds = await GetPhishingResistantStrengthPolicyIdsAsync();

                // Step 2: Get all CA policies
                var response = await _httpClient.GetAsync(
                    $"{GraphV1Url}/identity/conditionalAccess/policies");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get CA policies: {Status}", response.StatusCode);
                    return summary;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("value", out var policies) ||
                    policies.ValueKind != JsonValueKind.Array)
                    return summary;

                foreach (var policy in policies.EnumerateArray())
                {
                    // Check if this policy requires an auth strength that is phishing-resistant
                    if (!PolicyRequiresPhishingResistantStrength(policy, prStrengthIds))
                        continue;

                    // Determine policy state
                    var state = policy.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";
                    if (state == "enabled")
                        summary.EnforcingPolicies++;
                    else if (state == "enabledForReportingButNotEnforced")
                        summary.ReportOnlyPolicies++;
                    else
                        continue; // skip disabled policies

                    // Analyse user conditions
                    if (policy.TryGetProperty("conditions", out var conditions))
                    {
                        if (conditions.TryGetProperty("users", out var users))
                        {
                            if (users.TryGetProperty("includeUsers", out var incUsers) &&
                                incUsers.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var u in incUsers.EnumerateArray())
                                {
                                    if (u.GetString() == "All")
                                        summary.TargetsAllUsers = true;
                                }
                            }

                            if (users.TryGetProperty("includeGroups", out var incGroups) &&
                                incGroups.ValueKind == JsonValueKind.Array)
                                summary.TargetedGroupCount += incGroups.GetArrayLength();

                            if (users.TryGetProperty("excludeGroups", out var exGroups) &&
                                exGroups.ValueKind == JsonValueKind.Array)
                                summary.ExcludedGroupCount += exGroups.GetArrayLength();

                            if (users.TryGetProperty("excludeUsers", out var exUsers) &&
                                exUsers.ValueKind == JsonValueKind.Array)
                                summary.ExcludedGroupCount += exUsers.GetArrayLength();
                        }

                        if (conditions.TryGetProperty("applications", out var apps))
                        {
                            if (apps.TryGetProperty("includeApplications", out var incApps) &&
                                incApps.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var a in incApps.EnumerateArray())
                                {
                                    if (a.GetString() == "All")
                                        summary.TargetsAllApps = true;
                                }
                                summary.TargetedAppCount += incApps.GetArrayLength();
                            }

                            if (apps.TryGetProperty("excludeApplications", out var exApps) &&
                                exApps.ValueKind == JsonValueKind.Array)
                                summary.ExcludedAppCount += exApps.GetArrayLength();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching phishing-resistant enforcement policies");
            }

            return summary;
        }) ?? new PhishingResistantEnforcementSummary();
    }

    /// <summary>
    /// Gets authentication strength policy IDs that are phishing-resistant.
    /// The built-in "Phishing-resistant MFA" has a well-known ID, but we also check
    /// custom policies whose allowedCombinations include only PR methods.
    /// </summary>
    private async Task<HashSet<string>> GetPhishingResistantStrengthPolicyIdsAsync()
    {
        var prIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{GraphV1Url}/policies/authenticationStrengthPolicies");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get auth strength policies: {Status}", response.StatusCode);
                return prIds;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("value", out var policies) ||
                policies.ValueKind != JsonValueKind.Array)
                return prIds;

            // Known phishing-resistant auth method combinations
            var prMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "fido2",
                "x509CertificateMultiFactor",
                "windowsHelloForBusiness",
                "deviceBasedPush"
            };

            foreach (var policy in policies.EnumerateArray())
            {
                var id = policy.TryGetProperty("id", out var pId) ? pId.GetString() ?? "" : "";
                var policyType = policy.TryGetProperty("policyType", out var pt) ? pt.GetString() ?? "" : "";

                // The built-in phishing-resistant policy
                if (policyType == "builtIn")
                {
                    var displayName = policy.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "";
                    if (displayName.Contains("Phishing", StringComparison.OrdinalIgnoreCase))
                    {
                        prIds.Add(id);
                        continue;
                    }
                }

                // For custom policies, check if all allowed combinations are PR methods
                if (policy.TryGetProperty("allowedCombinations", out var combos) &&
                    combos.ValueKind == JsonValueKind.Array && combos.GetArrayLength() > 0)
                {
                    var allPr = true;
                    foreach (var combo in combos.EnumerateArray())
                    {
                        var comboStr = combo.GetString() ?? "";
                        var parts = comboStr.Split(',');
                        if (!parts.Any(p => prMethods.Contains(p.Trim())))
                        {
                            allPr = false;
                            break;
                        }
                    }
                    if (allPr)
                        prIds.Add(id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching auth strength policies");
        }

        return prIds;
    }

    /// <summary>
    /// Checks whether a CA policy's grant controls reference a phishing-resistant auth strength.
    /// </summary>
    private static bool PolicyRequiresPhishingResistantStrength(JsonElement policy, HashSet<string> prStrengthIds)
    {
        if (!policy.TryGetProperty("grantControls", out var grant) ||
            grant.ValueKind == JsonValueKind.Null)
            return false;

        // v1.0 Graph: grantControls.authenticationStrength.id
        if (grant.TryGetProperty("authenticationStrength", out var strength) &&
            strength.ValueKind != JsonValueKind.Null &&
            strength.TryGetProperty("id", out var strengthId))
        {
            return prStrengthIds.Contains(strengthId.GetString() ?? "");
        }

        return false;
    }
}
