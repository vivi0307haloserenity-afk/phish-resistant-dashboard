using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CredentialController : Controller
{
    private readonly IVerifiedIdService _verifiedIdService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<CredentialController> _logger;
    private const string Scope = "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access";

    public CredentialController(
        IVerifiedIdService verifiedIdService,
        ITokenAcquisition tokenAcquisition,
        ILogger<CredentialController> logger)
    {
        _verifiedIdService = verifiedIdService;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    [HttpGet("{authorityId}/{contractId}/{credentialId}")]
    public async Task<IActionResult> Get(string authorityId, string contractId, string credentialId)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var credential = await _verifiedIdService.GetCredentialAsync(accessToken, authorityId, contractId, credentialId);
            
            if (credential == null)
                return NotFound();
            
            return Ok(credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credential {CredentialId}", credentialId);
            return StatusCode(500, new { error = "Failed to retrieve credential" });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] CredentialSearchRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            
            // Compute the hash for searching
            var hashedClaimValue = _verifiedIdService.ComputeSearchHash(request.ContractId, request.ClaimValue);
            
            var credentials = await _verifiedIdService.SearchCredentialsAsync(
                accessToken, 
                request.AuthorityId, 
                request.ContractId, 
                hashedClaimValue);
            
            return Ok(credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching credentials");
            return StatusCode(500, new { error = "Failed to search credentials" });
        }
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] CredentialRevokeRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.RevokeCredentialAsync(
                accessToken, 
                request.AuthorityId, 
                request.ContractId, 
                request.CredentialId);
            
            if (!result)
                return BadRequest(new { error = "Failed to revoke credential" });
            
            return Ok(new { success = true, message = "Credential revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credential {CredentialId}", request.CredentialId);
            return StatusCode(500, new { error = "Failed to revoke credential" });
        }
    }

    [HttpPost("computeHash")]
    public IActionResult ComputeHash([FromBody] ComputeHashRequest request)
    {
        try
        {
            var hash = _verifiedIdService.ComputeSearchHash(request.ContractId, request.ClaimValue);
            return Ok(new { hash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing hash");
            return StatusCode(500, new { error = "Failed to compute hash" });
        }
    }
}

public class ComputeHashRequest
{
    public string ContractId { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}
