using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ContractController : Controller
{
    private readonly IVerifiedIdService _verifiedIdService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<ContractController> _logger;
    private const string Scope = "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access";

    public ContractController(
        IVerifiedIdService verifiedIdService,
        ITokenAcquisition tokenAcquisition,
        ILogger<ContractController> logger)
    {
        _verifiedIdService = verifiedIdService;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    [HttpGet("{authorityId}")]
    public async Task<IActionResult> GetAll(string authorityId)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var contracts = await _verifiedIdService.GetContractsAsync(accessToken, authorityId);
            return Ok(contracts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contracts for authority {AuthorityId}", authorityId);
            return StatusCode(500, new { error = "Failed to retrieve contracts" });
        }
    }

    [HttpGet("{authorityId}/{contractId}")]
    public async Task<IActionResult> Get(string authorityId, string contractId)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var contract = await _verifiedIdService.GetContractAsync(accessToken, authorityId, contractId);
            
            if (contract == null)
                return NotFound();
            
            return Ok(contract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract {ContractId}", contractId);
            return StatusCode(500, new { error = "Failed to retrieve contract" });
        }
    }

    [HttpPost("{authorityId}")]
    public async Task<IActionResult> Create(string authorityId, [FromBody] CreateContractRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var contract = await _verifiedIdService.CreateContractAsync(accessToken, authorityId, request);
            
            if (contract == null)
                return BadRequest(new { error = "Failed to create contract" });
            
            return CreatedAtAction(nameof(Get), new { authorityId, contractId = contract.Id }, contract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract");
            return StatusCode(500, new { error = "Failed to create contract" });
        }
    }

    [HttpPatch("{authorityId}/{contractId}")]
    public async Task<IActionResult> Update(string authorityId, string contractId, [FromBody] UpdateContractRequest request)
    {
        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Scope });
            var result = await _verifiedIdService.UpdateContractAsync(accessToken, authorityId, contractId, request);
            
            if (!result)
                return BadRequest(new { error = "Failed to update contract" });
            
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract {ContractId}", contractId);
            return StatusCode(500, new { error = "Failed to update contract" });
        }
    }
}
