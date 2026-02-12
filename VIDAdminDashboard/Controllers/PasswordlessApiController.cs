using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
[Route("api/passwordless")]
[ApiController]
public class PasswordlessApiController : ControllerBase
{
    private readonly IPasswordlessService _passwordlessService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<PasswordlessApiController> _logger;

    private static readonly string[] PolicyReadScopes = new[]
    {
        "https://graph.microsoft.com/Policy.Read.All"
    };

    private static readonly string[] PolicyWriteScopes = new[]
    {
        "https://graph.microsoft.com/Policy.ReadWrite.AuthenticationMethod"
    };

    private static readonly string[] ReportScopes = new[]
    {
        "https://graph.microsoft.com/Reports.Read.All"
    };

    private static readonly string[] SignInScopes = new[]
    {
        "https://graph.microsoft.com/AuditLog.Read.All"
    };

    public PasswordlessApiController(
        IPasswordlessService passwordlessService,
        ITokenAcquisition tokenAcquisition,
        ILogger<PasswordlessApiController> logger)
    {
        _passwordlessService = passwordlessService;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    // ─── FIDO2 ─────────────────────────────────────────────────────────

    [HttpGet("fido2")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Policy.Read.All" })]
    public async Task<IActionResult> GetFido2Configuration()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(PolicyReadScopes);
            var config = await _passwordlessService.GetFido2ConfigurationAsync(token);
            return config != null ? Ok(config) : StatusCode(500, new { error = "Failed to retrieve FIDO2 configuration" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FIDO2 configuration");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("fido2")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Policy.ReadWrite.AuthenticationMethod" })]
    public async Task<IActionResult> UpdateFido2Configuration([FromBody] Fido2Configuration config)
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(PolicyWriteScopes);
            var success = await _passwordlessService.UpdateFido2ConfigurationAsync(token, config);
            return success ? Ok(new { message = "FIDO2 configuration updated successfully" }) : StatusCode(500, new { error = "Failed to update FIDO2 configuration" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating FIDO2 configuration");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ─── Microsoft Authenticator ───────────────────────────────────────

    [HttpGet("authenticator")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Policy.Read.All" })]
    public async Task<IActionResult> GetAuthenticatorConfiguration()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(PolicyReadScopes);
            var config = await _passwordlessService.GetMicrosoftAuthenticatorConfigurationAsync(token);
            return config != null ? Ok(config) : StatusCode(500, new { error = "Failed to retrieve Authenticator configuration" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Authenticator configuration");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("authenticator")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Policy.ReadWrite.AuthenticationMethod" })]
    public async Task<IActionResult> UpdateAuthenticatorConfiguration([FromBody] MicrosoftAuthenticatorConfiguration config)
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(PolicyWriteScopes);
            var success = await _passwordlessService.UpdateMicrosoftAuthenticatorConfigurationAsync(token, config);
            return success ? Ok(new { message = "Authenticator configuration updated successfully" }) : StatusCode(500, new { error = "Failed to update Authenticator configuration" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Authenticator configuration");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ─── Aggregate Reports ─────────────────────────────────────────────

    [HttpGet("registration-by-feature")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Reports.Read.All" })]
    public async Task<IActionResult> GetRegistrationByFeature()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(ReportScopes);
            var summary = await _passwordlessService.GetRegistrationByFeatureAsync(token);
            return summary != null ? Ok(summary) : StatusCode(500, new { error = "Failed to retrieve feature summary" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration by feature");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("registration-by-method")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/Reports.Read.All" })]
    public async Task<IActionResult> GetRegistrationByMethod()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(ReportScopes);
            var methods = await _passwordlessService.GetRegistrationByMethodAsync(token);
            return Ok(methods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration by method");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("signin-by-method")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/AuditLog.Read.All" })]
    public async Task<IActionResult> GetRecentSignInsByMethod()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(SignInScopes);
            var signIns = await _passwordlessService.GetRecentSignInsByMethodAsync(token);
            return Ok(signIns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent sign-ins by method");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("recent-failures")]
    [AuthorizeForScopes(Scopes = new[] { "https://graph.microsoft.com/AuditLog.Read.All" })]
    public async Task<IActionResult> GetRecentFailures([FromQuery] int top = 25)
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(SignInScopes);
            var failures = await _passwordlessService.GetRecentSignInFailuresAsync(token, top);
            return Ok(failures);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent failures");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
