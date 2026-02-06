using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AuthorityController : Controller
{
    private readonly IVerifiedIdService _verifiedIdService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<AuthorityController> _logger;
    private const string Scope = "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access";
    private const string ArmScope = "https://management.azure.com/user_impersonation";

    public AuthorityController(
        IVerifiedIdService verifiedIdService,
        ITokenAcquisition tokenAcquisition,
        ILogger<AuthorityController> logger)
    {
        _verifiedIdService = verifiedIdService;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var authorities = await _verifiedIdService.GetAuthoritiesAsync(accessToken);
            return Ok(authorities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authorities");
            return StatusCode(500, new { error = "Failed to retrieve authorities" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var authority = await _verifiedIdService.GetAuthorityAsync(accessToken, id);
            
            if (authority == null)
                return NotFound();
            
            return Ok(authority);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to retrieve authority" });
        }
    }

    [HttpPost("{id}/generateDidDocument")]
    public async Task<IActionResult> GenerateDidDocument(string id)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.GenerateDidDocumentAsync(accessToken, id);
            
            if (result == null)
                return BadRequest(new { error = "Failed to generate DID document" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DID document for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to generate DID document" });
        }
    }

    [HttpPost("{id}/generateWellKnownDidConfiguration")]
    public async Task<IActionResult> GenerateWellKnownDidConfiguration(string id, [FromBody] GenerateWellKnownRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.GenerateWellKnownDidConfigurationAsync(accessToken, id, request.DomainUrl);
            
            if (result == null)
                return BadRequest(new { error = "Failed to generate well-known DID configuration" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating well-known DID configuration for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to generate well-known DID configuration" });
        }
    }

    [HttpPost("{id}/validateWellKnownDidConfiguration")]
    public async Task<IActionResult> ValidateWellKnownDidConfiguration(string id)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.ValidateWellKnownDidConfigurationAsync(accessToken, id);
            
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating well-known DID configuration for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to validate well-known DID configuration" });
        }
    }

    [HttpPost("{id}/rotateSigningKey")]
    public async Task<IActionResult> RotateSigningKey(string id)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.RotateSigningKeyAsync(accessToken, id);
            
            if (result == null)
                return BadRequest(new { error = "Failed to rotate signing key" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating signing key for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to rotate signing key" });
        }
    }

    [HttpPost("{id}/synchronizeWithDidDocument")]
    public async Task<IActionResult> SynchronizeWithDidDocument(string id)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.SynchronizeWithDidDocumentAsync(accessToken, id);
            
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing with DID document for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to synchronize with DID document" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuthorityRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var authority = await _verifiedIdService.CreateAuthorityAsync(accessToken, request);
            
            if (authority == null)
                return BadRequest(new { error = "Failed to create authority" });
            
            return CreatedAtAction(nameof(Get), new { id = authority.Id }, authority);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating authority");
            return StatusCode(500, new { error = "Failed to create authority" });
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAuthorityRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.UpdateAuthorityAsync(accessToken, id, request);
            
            if (!result)
                return BadRequest(new { error = "Failed to update authority" });
            
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to update authority" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.DeleteAuthorityAsync(accessToken, id);
            
            if (!result)
                return BadRequest(new { error = "Failed to delete authority" });
            
            return Ok(new { success = true, message = "Authority deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to delete authority" });
        }
    }

    // FaceCheck ARM API Endpoints
    [HttpGet("{id}/facecheck")]
    [AuthorizeForScopes(Scopes = new[] { "https://management.azure.com/user_impersonation" })]
    public async Task<IActionResult> GetFaceCheckStatus(string id, [FromQuery] string subscriptionId, [FromQuery] string resourceGroup)
    {
        try
        {
            if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup))
            {
                return BadRequest(new { error = "subscriptionId and resourceGroup are required" });
            }
            
            var armAccessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { ArmScope });
            var status = await _verifiedIdService.GetFaceCheckStatusAsync(armAccessToken, subscriptionId, resourceGroup, id);
            
            if (status == null)
                return StatusCode(500, new { error = "Failed to get FaceCheck status" });
            
            return Ok(new { 
                enabled = status.IsEnabled, 
                location = status.Location,
                provisioningState = status.Properties?.ProvisioningState 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FaceCheck status for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to get FaceCheck status" });
        }
    }

    [HttpPost("{id}/facecheck/enable")]
    [AuthorizeForScopes(Scopes = new[] { "https://management.azure.com/user_impersonation" })]
    public async Task<IActionResult> EnableFaceCheck(string id, [FromBody] EnableFaceCheckRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || 
                string.IsNullOrEmpty(request.ResourceGroup) || 
                string.IsNullOrEmpty(request.Location))
            {
                return BadRequest(new { error = "subscriptionId, resourceGroup, and location are required" });
            }
            
            var armAccessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { ArmScope });
            var result = await _verifiedIdService.EnableFaceCheckAsync(
                armAccessToken,
                request.SubscriptionId, 
                request.ResourceGroup, 
                id, 
                request.Location);
            
            if (!result)
                return BadRequest(new { error = "Failed to enable FaceCheck" });
            
            return Ok(new { success = true, message = "FaceCheck enabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling FaceCheck for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to enable FaceCheck" });
        }
    }

    [HttpPost("{id}/facecheck/disable")]
    [AuthorizeForScopes(Scopes = new[] { "https://management.azure.com/user_impersonation" })]
    public async Task<IActionResult> DisableFaceCheck(string id, [FromBody] DisableFaceCheckRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SubscriptionId) || string.IsNullOrEmpty(request.ResourceGroup))
            {
                return BadRequest(new { error = "subscriptionId and resourceGroup are required" });
            }
            
            var armAccessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { ArmScope });
            var result = await _verifiedIdService.DisableFaceCheckAsync(
                armAccessToken,
                request.SubscriptionId, 
                request.ResourceGroup, 
                id);
            
            if (!result)
                return BadRequest(new { error = "Failed to disable FaceCheck" });
            
            return Ok(new { success = true, message = "FaceCheck disabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling FaceCheck for authority {AuthorityId}", id);
            return StatusCode(500, new { error = "Failed to disable FaceCheck" });
        }
    }
}

public class GenerateWellKnownRequest
{
    public string DomainUrl { get; set; } = string.Empty;
}

public class EnableFaceCheckRequest
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class DisableFaceCheckRequest
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
}
